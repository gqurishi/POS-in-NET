-- Quick check: What orders are in the database?
SELECT 
    id,
    order_number,
    customer_name,
    created_at,
    sync_status,
    total_amount
FROM orders
WHERE sync_status = 'Synced'
ORDER BY created_at DESC
LIMIT 20;

-- Count by date
SELECT 
    DATE(created_at) as order_date,
    COUNT(*) as order_count,
    sync_status
FROM orders
WHERE sync_status = 'Synced'
GROUP BY DATE(created_at), sync_status
ORDER BY order_date DESC
LIMIT 30;
