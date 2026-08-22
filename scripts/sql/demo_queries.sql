-- 1) Trace full genealogy of demo lot
SELECT *
FROM demo_traceability
WHERE lot_id = 'LOT-DEMO-0822';

-- 2) Historical similar dimensional defects
SELECT *
FROM dimensional_history
WHERE equipment_id = 'INJ-04'
ORDER BY detected_at DESC;

-- 3) Blast radius / related lots
SELECT *
FROM lot_correlations
WHERE source_lot_id = 'LOT-DEMO-0822'
ORDER BY risk_score DESC;

-- 4) Recent NCs sharing same equipment
SELECT n.*
FROM nonconformities n
WHERE n.equipment_id = 'INJ-04'
  AND n.defect_type = 'DIMENSIONAL'
ORDER BY n.detected_at DESC;

-- 5) Audit trail for demo incident
SELECT *
FROM audit_events
WHERE entity_id = 'NC-DEMO-0822'
ORDER BY event_timestamp;
