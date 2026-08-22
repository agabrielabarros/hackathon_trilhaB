import random
import sqlite3
from pathlib import Path
from datetime import datetime, timedelta

SEED = 42

BASE_DIR = Path(__file__).resolve().parent.parent
DATA_DIR = BASE_DIR / "data"
DB_PATH = DATA_DIR / "rivexx_generated.db"


def iso(dt: datetime) -> str:
    return dt.strftime("%Y-%m-%dT%H:%M:%S")


def create_database():
    DATA_DIR.mkdir(parents=True, exist_ok=True)

    if DB_PATH.exists():
        DB_PATH.unlink()

    conn = sqlite3.connect(DB_PATH)
    conn.execute("PRAGMA foreign_keys = ON;")

    conn.executescript("""
    CREATE TABLE plants (
        plant_id TEXT PRIMARY KEY,
        plant_name TEXT NOT NULL,
        city TEXT NOT NULL,
        state TEXT NOT NULL
    );

    CREATE TABLE shifts (
        shift_id TEXT PRIMARY KEY,
        start_time TEXT NOT NULL,
        end_time TEXT NOT NULL
    );

    CREATE TABLE suppliers (
        supplier_id TEXT PRIMARY KEY,
        supplier_name TEXT NOT NULL,
        city TEXT NOT NULL,
        state TEXT NOT NULL,
        quality_rating INTEGER NOT NULL CHECK (quality_rating BETWEEN 0 AND 100)
    );

    CREATE TABLE equipment (
        equipment_id TEXT PRIMARY KEY,
        equipment_name TEXT NOT NULL,
        plant_id TEXT NOT NULL,
        line_id TEXT NOT NULL,
        equipment_type TEXT NOT NULL,
        mold_id TEXT NOT NULL,
        status TEXT NOT NULL,
        FOREIGN KEY (plant_id) REFERENCES plants(plant_id)
    );

    CREATE TABLE operators (
        operator_id TEXT PRIMARY KEY,
        operator_name TEXT NOT NULL,
        default_shift_id TEXT NOT NULL,
        active INTEGER NOT NULL CHECK (active IN (0, 1)),
        FOREIGN KEY (default_shift_id) REFERENCES shifts(shift_id)
    );

    CREATE TABLE quality_users (
        quality_user_id TEXT PRIMARY KEY,
        quality_user_name TEXT NOT NULL,
        role TEXT NOT NULL
    );

    CREATE TABLE raw_material_lots (
        raw_material_lot_id TEXT PRIMARY KEY,
        material_code TEXT NOT NULL,
        material_name TEXT NOT NULL,
        supplier_id TEXT NOT NULL,
        supplier_batch TEXT NOT NULL,
        received_at TEXT NOT NULL,
        certificate_status TEXT NOT NULL,
        moisture_pct REAL,
        FOREIGN KEY (supplier_id) REFERENCES suppliers(supplier_id)
    );

    CREATE TABLE production_lots (
        lot_id TEXT PRIMARY KEY,
        product_code TEXT NOT NULL,
        production_datetime TEXT NOT NULL,
        plant_id TEXT NOT NULL,
        line_id TEXT NOT NULL,
        equipment_id TEXT NOT NULL,
        mold_id TEXT NOT NULL,
        shift_id TEXT NOT NULL,
        raw_material_lot_id TEXT NOT NULL,
        quantity_units INTEGER NOT NULL,
        status TEXT NOT NULL,
        FOREIGN KEY (plant_id) REFERENCES plants(plant_id),
        FOREIGN KEY (equipment_id) REFERENCES equipment(equipment_id),
        FOREIGN KEY (shift_id) REFERENCES shifts(shift_id),
        FOREIGN KEY (raw_material_lot_id) REFERENCES raw_material_lots(raw_material_lot_id)
    );

    CREATE TABLE lot_operators (
        lot_id TEXT NOT NULL,
        operator_id TEXT NOT NULL,
        operator_role TEXT NOT NULL,
        started_at TEXT NOT NULL,
        ended_at TEXT,
        PRIMARY KEY (lot_id, operator_id, operator_role),
        FOREIGN KEY (lot_id) REFERENCES production_lots(lot_id),
        FOREIGN KEY (operator_id) REFERENCES operators(operator_id)
    );

    CREATE TABLE shipments (
        shipment_id TEXT PRIMARY KEY,
        lot_id TEXT NOT NULL,
        customer_name TEXT NOT NULL,
        destination_city TEXT NOT NULL,
        destination_state TEXT NOT NULL,
        shipped_at TEXT,
        quantity_units INTEGER NOT NULL,
        status TEXT NOT NULL,
        FOREIGN KEY (lot_id) REFERENCES production_lots(lot_id)
    );

    CREATE TABLE production_events (
        event_id TEXT PRIMARY KEY,
        lot_id TEXT NOT NULL,
        event_timestamp TEXT NOT NULL,
        event_type TEXT NOT NULL,
        equipment_id TEXT,
        operator_id TEXT,
        shift_id TEXT,
        details TEXT,
        FOREIGN KEY (lot_id) REFERENCES production_lots(lot_id),
        FOREIGN KEY (equipment_id) REFERENCES equipment(equipment_id),
        FOREIGN KEY (operator_id) REFERENCES operators(operator_id),
        FOREIGN KEY (shift_id) REFERENCES shifts(shift_id)
    );

    CREATE TABLE nonconformities (
        nc_id TEXT PRIMARY KEY,
        lot_id TEXT NOT NULL,
        detected_at TEXT NOT NULL,
        defect_type TEXT NOT NULL,
        severity TEXT NOT NULL,
        description TEXT NOT NULL,
        measured_value_mm REAL,
        specification_mm REAL,
        deviation_mm REAL,
        detected_by_operator_id TEXT NOT NULL,
        line_id TEXT NOT NULL,
        equipment_id TEXT NOT NULL,
        shift_id TEXT NOT NULL,
        evidence_uri TEXT,
        status TEXT NOT NULL,
        validated_root_cause_category TEXT,
        validated_root_cause TEXT,
        FOREIGN KEY (lot_id) REFERENCES production_lots(lot_id),
        FOREIGN KEY (detected_by_operator_id) REFERENCES operators(operator_id),
        FOREIGN KEY (equipment_id) REFERENCES equipment(equipment_id),
        FOREIGN KEY (shift_id) REFERENCES shifts(shift_id)
    );

    CREATE TABLE corrective_actions (
        action_id TEXT PRIMARY KEY,
        nc_id TEXT NOT NULL,
        action_type TEXT NOT NULL,
        description TEXT NOT NULL,
        owner_quality_user_id TEXT NOT NULL,
        created_at TEXT NOT NULL,
        due_date TEXT NOT NULL,
        status TEXT NOT NULL,
        verified_at TEXT,
        FOREIGN KEY (nc_id) REFERENCES nonconformities(nc_id),
        FOREIGN KEY (owner_quality_user_id) REFERENCES quality_users(quality_user_id)
    );

    CREATE TABLE lot_correlations (
        source_lot_id TEXT NOT NULL,
        related_lot_id TEXT NOT NULL,
        risk_score INTEGER NOT NULL,
        risk_level TEXT NOT NULL,
        relationship_reasons TEXT NOT NULL,
        PRIMARY KEY (source_lot_id, related_lot_id),
        FOREIGN KEY (source_lot_id) REFERENCES production_lots(lot_id),
        FOREIGN KEY (related_lot_id) REFERENCES production_lots(lot_id)
    );

    CREATE TABLE audit_events (
        audit_event_id TEXT PRIMARY KEY,
        event_timestamp TEXT NOT NULL,
        actor_type TEXT NOT NULL,
        actor_id TEXT NOT NULL,
        entity_type TEXT NOT NULL,
        entity_id TEXT NOT NULL,
        action TEXT NOT NULL,
        details TEXT
    );

    CREATE INDEX idx_raw_material_supplier
        ON raw_material_lots(supplier_id);

    CREATE INDEX idx_production_lot_equipment
        ON production_lots(equipment_id);

    CREATE INDEX idx_production_lot_raw_material
        ON production_lots(raw_material_lot_id);

    CREATE INDEX idx_lot_operators_lot
        ON lot_operators(lot_id);

    CREATE INDEX idx_production_events_lot
        ON production_events(lot_id);

    CREATE INDEX idx_nc_lot
        ON nonconformities(lot_id);

    CREATE INDEX idx_nc_equipment
        ON nonconformities(equipment_id);

    CREATE INDEX idx_nc_defect
        ON nonconformities(defect_type);

    CREATE INDEX idx_corrective_actions_nc
        ON corrective_actions(nc_id);

    CREATE INDEX idx_correlations_source
        ON lot_correlations(source_lot_id);
    """)

    return conn


def generate_master_data(conn):
    plants = [
        ("PLANT-01", "Rivexx Plant 1", "Contagem", "MG"),
        ("PLANT-02", "Rivexx Plant 2", "Betim", "MG"),
    ]
    conn.executemany(
        "INSERT INTO plants VALUES (?, ?, ?, ?)",
        plants,
    )

    shifts = [
        ("A", "06:00", "14:00"),
        ("B", "14:00", "22:00"),
        ("C", "22:00", "06:00"),
    ]
    conn.executemany(
        "INSERT INTO shifts VALUES (?, ?, ?)",
        shifts,
    )

    suppliers = [
        ("SUP-001", "Polimix Resinas", "Betim", "MG", 94),
        ("SUP-002", "TecnoPolimeros", "Contagem", "MG", 91),
        ("SUP-003", "NovaPlast Materiais", "Campinas", "SP", 88),
        ("SUP-004", "ResinTech Brasil", "Jundiai", "SP", 96),
        ("SUP-005", "Prime Polymer", "Sorocaba", "SP", 89),
        ("SUP-006", "MG Compostos", "Belo Horizonte", "MG", 92),
    ]
    conn.executemany(
        "INSERT INTO suppliers VALUES (?, ?, ?, ?, ?)",
        suppliers,
    )

    equipment = []
    for line in range(1, 7):
        plant_id = "PLANT-01" if line <= 3 else "PLANT-02"
        equipment.append(
            (
                f"INJ-{line:02d}",
                f"Injetora Linha {line}",
                plant_id,
                f"L{line}",
                "Injection molding",
                f"M{line:02d}",
                "ACTIVE",
            )
        )
    conn.executemany(
        """
        INSERT INTO equipment (
            equipment_id, equipment_name, plant_id, line_id,
            equipment_type, mold_id, status
        ) VALUES (?, ?, ?, ?, ?, ?, ?)
        """,
        equipment,
    )

    first_names = [
        "Ana", "Bruno", "Carlos", "Daniela", "Eduardo", "Fernanda",
        "Gabriel", "Helena", "Igor", "Julia", "Kaio", "Larissa"
    ]
    last_names = [
        "Silva", "Souza", "Oliveira", "Costa",
        "Almeida", "Ferreira", "Lima", "Rocha"
    ]
    operators = []
    for i in range(1, 31):
        operators.append(
            (
                f"OP-{i:03d}",
                f"{random.choice(first_names)} {random.choice(last_names)}",
                random.choice(["A", "B", "C"]),
                1,
            )
        )
    conn.executemany(
        "INSERT INTO operators VALUES (?, ?, ?, ?)",
        operators,
    )

    quality_users = [
        ("Q-001", "Marina Ribeiro", "Quality Analyst"),
        ("Q-002", "Rafael Martins", "Quality Engineer"),
        ("Q-003", "Camila Nunes", "Quality Coordinator"),
    ]
    conn.executemany(
        "INSERT INTO quality_users VALUES (?, ?, ?)",
        quality_users,
    )


def generate_raw_material_lots(conn):
    materials = [
        ("MAT-PA66-GF30", "PA66 GF30"),
        ("MAT-PP-HI", "Polipropileno alto impacto"),
        ("MAT-ABS-01", "ABS tecnico"),
        ("MAT-POM-01", "POM acetal"),
    ]

    start_date = datetime(2026, 6, 15, 8, 0)
    rows = []

    for i in range(1, 101):
        material_code, material_name = random.choice(materials)
        supplier_id = f"SUP-{random.randint(1, 6):03d}"
        received_at = start_date + timedelta(hours=random.randint(0, 24 * 65))

        rows.append(
            (
                f"RM-{i:04d}",
                material_code,
                material_name,
                supplier_id,
                f"{supplier_id[-3:]}-{received_at:%y%m%d}-{i:03d}",
                iso(received_at),
                random.choices(
                    ["APPROVED", "CONDITIONAL"],
                    weights=[96, 4],
                    k=1,
                )[0],
                round(random.uniform(0.05, 0.35), 3),
            )
        )

    # Fixed demo material lot.
    rows.append(
        (
            "RM-DEMO-0822",
            "MAT-PA66-GF30",
            "PA66 GF30",
            "SUP-002",
            "002-260820-DEMO",
            "2026-08-20T09:15:00",
            "APPROVED",
            0.128,
        )
    )

    conn.executemany(
        """
        INSERT INTO raw_material_lots (
            raw_material_lot_id, material_code, material_name,
            supplier_id, supplier_batch, received_at,
            certificate_status, moisture_pct
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        """,
        rows,
    )


def generate_production_lots(conn):
    products = ["P-A110", "P-A120", "P-B210", "P-C330", "P-D410"]
    start_date = datetime(2026, 7, 1, 6, 0)

    rows = []

    for i in range(1, 251):
        line = random.randint(1, 6)
        plant_id = "PLANT-01" if line <= 3 else "PLANT-02"

        rows.append(
            (
                f"LOT-{i:05d}",
                random.choice(products),
                iso(start_date + timedelta(hours=random.randint(0, 24 * 51))),
                plant_id,
                f"L{line}",
                f"INJ-{line:02d}",
                f"M{line:02d}",
                random.choice(["A", "B", "C"]),
                f"RM-{random.randint(1, 100):04d}",
                random.randint(800, 6000),
                "RELEASED",
            )
        )

    # Controlled line-4 history used by RCA.
    cluster_dates = [
        datetime(2026, 8, 12, 15, 10),
        datetime(2026, 8, 14, 16, 20),
        datetime(2026, 8, 16, 15, 5),
        datetime(2026, 8, 18, 17, 40),
        datetime(2026, 8, 20, 14, 30),
        datetime(2026, 8, 21, 16, 15),
    ]

    for j, dt in enumerate(cluster_dates, start=1):
        rows.append(
            (
                f"LOT-L4-{j:03d}",
                "P-A110",
                iso(dt),
                "PLANT-02",
                "L4",
                "INJ-04",
                "M04",
                "B",
                "RM-DEMO-0822" if j >= 4 else f"RM-{70 + j:04d}",
                3200 + j * 100,
                "RELEASED",
            )
        )

    # Official demo lot.
    rows.append(
        (
            "LOT-DEMO-0822",
            "P-A110",
            "2026-08-22T15:05:00",
            "PLANT-02",
            "L4",
            "INJ-04",
            "M04",
            "B",
            "RM-DEMO-0822",
            3600,
            "HOLD",
        )
    )

    conn.executemany(
        """
        INSERT INTO production_lots (
            lot_id, product_code, production_datetime,
            plant_id, line_id, equipment_id, mold_id,
            shift_id, raw_material_lot_id,
            quantity_units, status
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """,
        rows,
    )


def generate_lot_operators(conn):
    lots = conn.execute(
        """
        SELECT lot_id, production_datetime, shift_id
        FROM production_lots
        """
    ).fetchall()

    rows = []

    for lot_id, production_datetime, shift_id in lots:
        dt = datetime.fromisoformat(production_datetime)

        # One production operator.
        primary_operator = random.randint(1, 30)
        rows.append(
            (
                lot_id,
                f"OP-{primary_operator:03d}",
                "PRODUCTION",
                iso(dt),
                iso(dt + timedelta(minutes=90)),
            )
        )

        # Some lots also have setup and/or inspection operators.
        used = {primary_operator}

        if random.random() < 0.55:
            setup_operator = random.choice(
                [x for x in range(1, 31) if x not in used]
            )
            used.add(setup_operator)
            rows.append(
                (
                    lot_id,
                    f"OP-{setup_operator:03d}",
                    "SETUP",
                    iso(dt - timedelta(minutes=25)),
                    iso(dt),
                )
            )

        if random.random() < 0.65:
            inspection_operator = random.choice(
                [x for x in range(1, 31) if x not in used]
            )
            rows.append(
                (
                    lot_id,
                    f"OP-{inspection_operator:03d}",
                    "INSPECTION",
                    iso(dt + timedelta(minutes=75)),
                    iso(dt + timedelta(minutes=105)),
                )
            )

    # Replace demo-lot operators with fixed auditable actors.
    rows = [row for row in rows if row[0] != "LOT-DEMO-0822"]
    rows.extend(
        [
            (
                "LOT-DEMO-0822",
                "OP-019",
                "PRODUCTION",
                "2026-08-22T15:05:00",
                "2026-08-22T16:20:00",
            ),
            (
                "LOT-DEMO-0822",
                "OP-008",
                "SETUP",
                "2026-08-22T14:40:00",
                "2026-08-22T15:05:00",
            ),
            (
                "LOT-DEMO-0822",
                "OP-025",
                "INSPECTION",
                "2026-08-22T16:10:00",
                "2026-08-22T16:30:00",
            ),
        ]
    )

    conn.executemany(
        """
        INSERT INTO lot_operators (
            lot_id, operator_id, operator_role,
            started_at, ended_at
        ) VALUES (?, ?, ?, ?, ?)
        """,
        rows,
    )


def generate_shipments(conn):
    customer_locations = [
        ("AutoMotion", "Betim", "MG"),
        ("Electra Systems", "Contagem", "MG"),
        ("MobiParts", "Sao Paulo", "SP"),
        ("Vector Automotive", "Sao Bernardo do Campo", "SP"),
        ("TechDrive", "Campinas", "SP"),
    ]

    lots = conn.execute(
        """
        SELECT lot_id, production_datetime, quantity_units, status
        FROM production_lots
        """
    ).fetchall()

    rows = []
    shipment_counter = 1

    for lot_id, production_datetime, quantity_units, lot_status in lots:
        dt = datetime.fromisoformat(production_datetime)

        if lot_id == "LOT-DEMO-0822":
            rows.append(
                (
                    "SHP-DEMO-0822",
                    lot_id,
                    "AutoMotion",
                    "Betim",
                    "MG",
                    None,
                    quantity_units,
                    "BLOCKED",
                )
            )
            continue

        customer_name, city, state = random.choice(customer_locations)
        rows.append(
            (
                f"SHP-{shipment_counter:05d}",
                lot_id,
                customer_name,
                city,
                state,
                iso(dt + timedelta(hours=random.randint(6, 72))),
                quantity_units,
                "SHIPPED" if lot_status == "RELEASED" else "BLOCKED",
            )
        )
        shipment_counter += 1

    conn.executemany(
        """
        INSERT INTO shipments (
            shipment_id, lot_id, customer_name,
            destination_city, destination_state,
            shipped_at, quantity_units, status
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        """,
        rows,
    )


def generate_production_events(conn):
    lots = conn.execute(
        """
        SELECT
            p.lot_id,
            p.production_datetime,
            p.equipment_id,
            p.shift_id,
            p.raw_material_lot_id,
            p.status,
            lo.operator_id
        FROM production_lots p
        LEFT JOIN lot_operators lo
          ON p.lot_id = lo.lot_id
         AND lo.operator_role = 'PRODUCTION'
        """
    ).fetchall()

    rows = []
    event_counter = 1

    for (
        lot_id,
        production_datetime,
        equipment_id,
        shift_id,
        raw_material_lot_id,
        status,
        operator_id,
    ) in lots:
        dt = datetime.fromisoformat(production_datetime)

        events = [
            (
                "MATERIAL_ISSUED",
                -35,
                f"Raw material {raw_material_lot_id} issued to {equipment_id}",
            ),
            (
                "PRODUCTION_STARTED",
                0,
                f"Production started on {equipment_id}",
            ),
            (
                "PRODUCTION_FINISHED",
                75,
                f"Lot {lot_id} completed",
            ),
            (
                "QUALITY_RELEASE",
                120,
                f"Lot status set to {status}",
            ),
        ]

        for event_type, offset, details in events:
            rows.append(
                (
                    f"PE-{event_counter:06d}",
                    lot_id,
                    iso(dt + timedelta(minutes=offset)),
                    event_type,
                    equipment_id,
                    operator_id,
                    shift_id,
                    details,
                )
            )
            event_counter += 1

    conn.executemany(
        """
        INSERT INTO production_events (
            event_id, lot_id, event_timestamp,
            event_type, equipment_id, operator_id,
            shift_id, details
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        """,
        rows,
    )


def generate_nonconformities(conn):
    defects = [
        ("DIMENSIONAL", "Dimensao fora da tolerancia"),
        ("VISUAL", "Rebarba ou falha superficial"),
        ("COLOR", "Variacao de cor"),
        ("SHORT_SHOT", "Preenchimento incompleto"),
        ("WARPAGE", "Empenamento"),
    ]

    root_causes = [
        ("MACHINE", "Parameter drift"),
        ("MATERIAL", "Material condition"),
        ("METHOD", "Setup inconsistency"),
        ("MOLD", "Mold wear"),
        ("MEASUREMENT", "Measurement variation"),
    ]

    lots = conn.execute(
        """
        SELECT
            p.lot_id,
            p.production_datetime,
            p.line_id,
            p.equipment_id,
            p.shift_id,
            COALESCE(lo.operator_id, 'OP-001')
        FROM production_lots p
        LEFT JOIN lot_operators lo
          ON p.lot_id = lo.lot_id
         AND lo.operator_role = 'PRODUCTION'
        WHERE p.lot_id LIKE 'LOT-%'
          AND p.lot_id NOT LIKE 'LOT-L4-%'
          AND p.lot_id != 'LOT-DEMO-0822'
        """
    ).fetchall()

    rows = []
    nc_counter = 1

    # Background noise/history.
    for _ in range(75):
        (
            lot_id,
            production_datetime,
            line_id,
            equipment_id,
            shift_id,
            operator_id,
        ) = random.choice(lots)

        detected_at = (
            datetime.fromisoformat(production_datetime)
            + timedelta(minutes=random.randint(30, 180))
        )

        defect_type, defect_description = random.choice(defects)
        severity = random.choices(
            ["LOW", "MEDIUM", "HIGH"],
            weights=[45, 40, 15],
            k=1,
        )[0]

        root_cause_category, root_cause = random.choice(root_causes)

        if defect_type == "DIMENSIONAL":
            specification = 20.0
            deviation = round(random.uniform(-0.55, 0.65), 2)
            measured = round(specification + deviation, 2)
        else:
            specification = None
            deviation = None
            measured = None

        rows.append(
            (
                f"NC-{nc_counter:05d}",
                lot_id,
                iso(detected_at),
                defect_type,
                severity,
                defect_description,
                measured,
                specification,
                deviation,
                operator_id,
                line_id,
                equipment_id,
                shift_id,
                f"/evidence/NC-{nc_counter:05d}.jpg",
                random.choice(["CLOSED", "CLOSED", "CLOSED", "IN_ANALYSIS"]),
                root_cause_category,
                root_cause,
            )
        )
        nc_counter += 1

    # Controlled RCA signal: progressive dimensional drift on line 4 / M04.
    cluster_lots = conn.execute(
        """
        SELECT
            p.lot_id,
            p.production_datetime,
            lo.operator_id
        FROM production_lots p
        JOIN lot_operators lo
          ON p.lot_id = lo.lot_id
         AND lo.operator_role = 'PRODUCTION'
        WHERE p.lot_id LIKE 'LOT-L4-%'
        ORDER BY p.production_datetime
        """
    ).fetchall()

    for i, (lot_id, production_datetime, operator_id) in enumerate(
        cluster_lots,
        start=1,
    ):
        detected_at = (
            datetime.fromisoformat(production_datetime)
            + timedelta(minutes=85)
        )

        deviation = round(0.28 + i * 0.07, 2)

        rows.append(
            (
                f"NC-L4-{i:03d}",
                lot_id,
                iso(detected_at),
                "DIMENSIONAL",
                "MEDIUM" if i < 5 else "HIGH",
                f"Diametro acima da tolerancia em +{deviation:.2f} mm",
                round(20.0 + deviation, 2),
                20.0,
                deviation,
                operator_id,
                "L4",
                "INJ-04",
                "B",
                f"/evidence/NC-L4-{i:03d}.jpg",
                "CLOSED",
                "MOLD",
                "Progressive wear of mold M04",
            )
        )

    # Open demo NC: root cause intentionally unknown.
    rows.append(
        (
            "NC-DEMO-0822",
            "LOT-DEMO-0822",
            "2026-08-22T16:22:00",
            "DIMENSIONAL",
            "HIGH",
            "Diametro da peca 0.80 mm acima da tolerancia na Linha 4",
            20.80,
            20.00,
            0.80,
            "OP-025",
            "L4",
            "INJ-04",
            "B",
            "/evidence/NC-DEMO-0822.jpg",
            "OPEN",
            None,
            None,
        )
    )

    conn.executemany(
        """
        INSERT INTO nonconformities (
            nc_id, lot_id, detected_at, defect_type,
            severity, description, measured_value_mm,
            specification_mm, deviation_mm,
            detected_by_operator_id, line_id,
            equipment_id, shift_id, evidence_uri,
            status, validated_root_cause_category,
            validated_root_cause
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """,
        rows,
    )


def generate_corrective_actions(conn):
    historical_ncs = conn.execute(
        """
        SELECT nc_id, detected_at
        FROM nonconformities
        WHERE nc_id != 'NC-DEMO-0822'
        """
    ).fetchall()

    descriptions = [
        "Review machine parameters and standard setup",
        "Inspect mold condition before next production run",
        "Segregate affected lot and perform 100% inspection",
        "Review raw material certificate and storage conditions",
        "Calibrate measurement instrument",
    ]

    rows = []

    for i, (nc_id, detected_at) in enumerate(historical_ncs, start=1):
        dt = datetime.fromisoformat(detected_at)
        verified = (
            iso(dt + timedelta(days=random.randint(2, 8)))
            if random.random() < 0.72
            else None
        )

        rows.append(
            (
                f"CA-{i:05d}",
                nc_id,
                random.choice(["CORRECTIVE", "CONTAINMENT", "PREVENTIVE"]),
                random.choice(descriptions),
                f"Q-{random.randint(1, 3):03d}",
                iso(dt + timedelta(hours=2)),
                (dt.date() + timedelta(days=random.randint(1, 7))).isoformat(),
                random.choice(["DONE", "DONE", "IN_PROGRESS"]),
                verified,
            )
        )

    conn.executemany(
        """
        INSERT INTO corrective_actions (
            action_id, nc_id, action_type,
            description, owner_quality_user_id,
            created_at, due_date, status,
            verified_at
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        """,
        rows,
    )


def generate_lot_correlations(conn):
    demo = conn.execute(
        """
        SELECT
            production_datetime,
            equipment_id,
            mold_id,
            raw_material_lot_id
        FROM production_lots
        WHERE lot_id = 'LOT-DEMO-0822'
        """
    ).fetchone()

    demo_time = datetime.fromisoformat(demo[0])
    demo_equipment = demo[1]
    demo_mold = demo[2]
    demo_raw_material = demo[3]

    lots = conn.execute(
        """
        SELECT
            lot_id,
            production_datetime,
            equipment_id,
            mold_id,
            raw_material_lot_id
        FROM production_lots
        WHERE lot_id != 'LOT-DEMO-0822'
        """
    ).fetchall()

    rows = []

    for (
        lot_id,
        production_datetime,
        equipment_id,
        mold_id,
        raw_material_lot_id,
    ) in lots:
        reasons = []
        score = 0

        if raw_material_lot_id == demo_raw_material:
            reasons.append("SAME_RAW_MATERIAL_LOT")
            score += 50

        if equipment_id == demo_equipment:
            reasons.append("SAME_EQUIPMENT")
            score += 25

        if mold_id == demo_mold:
            reasons.append("SAME_MOLD")
            score += 20

        lot_time = datetime.fromisoformat(production_datetime)
        if abs((demo_time - lot_time).total_seconds()) <= 7 * 86400:
            reasons.append("WITHIN_7_DAYS")
            score += 10

        if score < 30:
            continue

        risk_score = min(score, 100)

        if risk_score >= 70:
            risk_level = "HIGH"
        elif risk_score >= 50:
            risk_level = "MEDIUM"
        else:
            risk_level = "LOW"

        rows.append(
            (
                "LOT-DEMO-0822",
                lot_id,
                risk_score,
                risk_level,
                "|".join(reasons),
            )
        )

    conn.executemany(
        """
        INSERT INTO lot_correlations (
            source_lot_id,
            related_lot_id,
            risk_score,
            risk_level,
            relationship_reasons
        ) VALUES (?, ?, ?, ?, ?)
        """,
        rows,
    )


def generate_audit_events(conn):
    rows = []
    counter = 1

    ncs = conn.execute(
        """
        SELECT
            nc_id,
            detected_at,
            detected_by_operator_id,
            defect_type,
            equipment_id,
            shift_id
        FROM nonconformities
        """
    ).fetchall()

    for (
        nc_id,
        detected_at,
        operator_id,
        defect_type,
        equipment_id,
        shift_id,
    ) in ncs:
        rows.append(
            (
                f"AE-{counter:06d}",
                detected_at,
                "OPERATOR",
                operator_id,
                "NONCONFORMITY",
                nc_id,
                "CREATED",
                f"{defect_type} / {equipment_id} / shift {shift_id}",
            )
        )
        counter += 1

    demo_flow = [
        (
            "2026-08-22T16:22:04",
            "SYSTEM",
            "RCA-ENGINE",
            "NONCONFORMITY",
            "NC-DEMO-0822",
            "CONTEXT_ENRICHED",
            "Lot, material, supplier, equipment, shift, operators and historical cases correlated",
        ),
        (
            "2026-08-22T16:22:06",
            "AI",
            "RCA-AGENT",
            "NONCONFORMITY",
            "NC-DEMO-0822",
            "ROOT_CAUSE_SUGGESTED",
            "Progressive wear of mold M04; confidence=0.87; evidence based on six prior similar cases",
        ),
    ]

    for event in demo_flow:
        rows.append(
            (
                f"AE-{counter:06d}",
                *event,
            )
        )
        counter += 1

    conn.executemany(
        """
        INSERT INTO audit_events (
            audit_event_id, event_timestamp,
            actor_type, actor_id,
            entity_type, entity_id,
            action, details
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        """,
        rows,
    )


def create_views(conn):
    conn.executescript("""
    CREATE VIEW demo_traceability AS
    SELECT
        p.lot_id,
        p.product_code,
        p.production_datetime,
        p.plant_id,
        pl.plant_name,
        p.line_id,
        p.equipment_id,
        e.equipment_name,
        p.mold_id,
        p.shift_id,
        p.raw_material_lot_id,
        r.material_code,
        r.material_name,
        r.supplier_id,
        s.supplier_name,
        sh.shipment_id,
        sh.customer_name,
        sh.destination_city,
        sh.destination_state,
        sh.shipped_at,
        sh.status AS shipment_status,
        p.status AS lot_status
    FROM production_lots p
    JOIN plants pl
      ON p.plant_id = pl.plant_id
    JOIN equipment e
      ON p.equipment_id = e.equipment_id
    JOIN raw_material_lots r
      ON p.raw_material_lot_id = r.raw_material_lot_id
    JOIN suppliers s
      ON r.supplier_id = s.supplier_id
    LEFT JOIN shipments sh
      ON p.lot_id = sh.lot_id;

    CREATE VIEW dimensional_history AS
    SELECT
        n.nc_id,
        n.lot_id,
        n.detected_at,
        n.line_id,
        n.equipment_id,
        p.mold_id,
        n.shift_id,
        n.deviation_mm,
        n.validated_root_cause_category,
        n.validated_root_cause
    FROM nonconformities n
    JOIN production_lots p
      ON n.lot_id = p.lot_id
    WHERE n.defect_type = 'DIMENSIONAL';

    CREATE VIEW demo_lot_operators AS
    SELECT
        lo.lot_id,
        lo.operator_role,
        lo.operator_id,
        o.operator_name,
        lo.started_at,
        lo.ended_at
    FROM lot_operators lo
    JOIN operators o
      ON lo.operator_id = o.operator_id;
    """)


def print_summary(conn):
    tables = [
        "plants",
        "shifts",
        "suppliers",
        "equipment",
        "operators",
        "quality_users",
        "raw_material_lots",
        "production_lots",
        "lot_operators",
        "shipments",
        "production_events",
        "nonconformities",
        "corrective_actions",
        "lot_correlations",
        "audit_events",
    ]

    print("\nGenerated rows:")
    for table in tables:
        count = conn.execute(
            f"SELECT COUNT(*) FROM {table}"
        ).fetchone()[0]
        print(f"  {table:<24} {count}")


def main():
    random.seed(SEED)

    print("Generating Rivexx synthetic dataset...")

    conn = create_database()

    generate_master_data(conn)
    generate_raw_material_lots(conn)
    generate_production_lots(conn)
    generate_lot_operators(conn)
    generate_shipments(conn)
    generate_production_events(conn)
    generate_nonconformities(conn)
    generate_corrective_actions(conn)
    generate_lot_correlations(conn)
    generate_audit_events(conn)
    create_views(conn)

    conn.commit()

    print_summary(conn)

    conn.close()

    print("\nSynthetic database generated successfully.")
    print(f"Database: {DB_PATH}")


if __name__ == "__main__":
    main()