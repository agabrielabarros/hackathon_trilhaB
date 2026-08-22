from .database import get_connection


def get_lot_traceability(lot_id: str):
    conn = get_connection()

    row = conn.execute(
        """
        SELECT *
        FROM demo_traceability
        WHERE lot_id = ?
        """,
        (lot_id,),
    ).fetchone()

    conn.close()

    if row is None:
        return None

    return dict(row)