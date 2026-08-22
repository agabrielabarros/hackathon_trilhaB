"""Single choke point for every model call.

Everything the squad says to the model goes through here, so swapping providers
or dropping into offline mock mode is a one-line change on demo day.
"""
import json
import os
import re
import time

from dotenv import load_dotenv

load_dotenv()

MODEL = os.getenv("ANTHROPIC_MODEL", "claude-haiku-4-5-20251001")
MODE = os.getenv("SQUAD_MODE", "live").lower()  # live | mock
MAX_TOKENS = int(os.getenv("SQUAD_MAX_TOKENS", "8000"))
MAX_TOKENS_CODIGO = int(os.getenv("SQUAD_MAX_TOKENS_CODIGO", "16000"))

_client = None


class LLMError(RuntimeError):
    pass


def available() -> bool:
    return MODE == "mock" or bool(os.getenv("ANTHROPIC_API_KEY"))


def _get_client():
    global _client
    if _client is None:
        import anthropic

        key = os.getenv("ANTHROPIC_API_KEY")
        if not key:
            raise LLMError(
                "ANTHROPIC_API_KEY não encontrada. Defina no .env ou rode com SQUAD_MODE=mock."
            )
        _client = anthropic.Anthropic(api_key=key)
    return _client


def _strip_fences(text: str) -> str:
    text = text.strip()
    text = re.sub(r"^```(?:json|html)?\s*", "", text)
    text = re.sub(r"\s*```$", "", text)
    return text.strip()


def _extract_json(text: str):
    text = _strip_fences(text)
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        pass
    # Model prefaced the JSON with prose — grab the outermost object/array.
    for opener, closer in (("{", "}"), ("[", "]")):
        start, end = text.find(opener), text.rfind(closer)
        if start != -1 and end > start:
            try:
                return json.loads(text[start : end + 1])
            except json.JSONDecodeError:
                continue
    raise LLMError(f"Resposta não é JSON válido:\n{text[:400]}")


def call_json(system: str, user: str, *, prefill: str = "{", retries: int = 2) -> tuple[dict, dict]:
    """Returns (parsed_json, usage). Assistant prefill forces the model straight
    into JSON instead of an explanatory preamble."""
    if MODE == "mock":
        from app.squad import mock

        return mock.respond(system, user), {"input_tokens": 0, "output_tokens": 0}

    client = _get_client()
    last_error = None
    for attempt in range(retries + 1):
        try:
            resp = client.messages.create(
                model=MODEL,
                max_tokens=MAX_TOKENS,
                system=system,
                messages=[
                    {"role": "user", "content": user},
                    {"role": "assistant", "content": prefill},
                ],
            )
            raw = prefill + "".join(b.text for b in resp.content if b.type == "text")
            usage = {
                "input_tokens": resp.usage.input_tokens,
                "output_tokens": resp.usage.output_tokens,
            }
            return _extract_json(raw), usage
        except LLMError as exc:
            last_error = exc
            user += (
                "\n\nSua resposta anterior não era JSON válido. "
                "Responda APENAS com o objeto JSON, sem texto ao redor."
            )
        except Exception as exc:  # network / rate limit
            last_error = exc
            time.sleep(1.5 * (attempt + 1))
    raise LLMError(f"Falha após {retries + 1} tentativas: {last_error}")


def call_text(system: str, user: str, *, prefill: str = "", retries: int = 2) -> tuple[str, dict]:
    """Raw text out. Used for the Dev Agent's file-writing call: asking a model to
    embed a whole HTML document inside a JSON string is the most reliable way to
    get malformed JSON, so we simply don't."""
    if MODE == "mock":
        from app.squad import mock

        return mock.respond_text(system, user), {"input_tokens": 0, "output_tokens": 0}

    client = _get_client()
    last_error = None
    for attempt in range(retries + 1):
        try:
            mensagens = [{"role": "user", "content": user}]
            if prefill:
                mensagens.append({"role": "assistant", "content": prefill})
            resp = client.messages.create(
                model=MODEL, max_tokens=MAX_TOKENS_CODIGO, system=system, messages=mensagens
            )
            texto = prefill + "".join(b.text for b in resp.content if b.type == "text")
            usage = {"input_tokens": resp.usage.input_tokens,
                     "output_tokens": resp.usage.output_tokens,
                     "stop_reason": resp.stop_reason}
            return _strip_fences(texto), usage
        except Exception as exc:
            last_error = exc
            time.sleep(1.5 * (attempt + 1))
    raise LLMError(f"Falha após {retries + 1} tentativas: {last_error}")
