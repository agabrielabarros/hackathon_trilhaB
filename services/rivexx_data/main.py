from fastapi import FastAPI, HTTPException

from .repository import get_lot_traceability


app = FastAPI(
    title="Rivexx Data Service",
    version="0.1.0",
)


@app.get("/health")
def health():
    return {
        "status": "ok",
        "service": "rivexx-data",
    }


@app.get("/lots/{lot_id}")
def trace_lot(lot_id: str):
    lot = get_lot_traceability(lot_id)

    if lot is None:
        raise HTTPException(
            status_code=404,
            detail="Lote nao encontrado",
        )

    return lot