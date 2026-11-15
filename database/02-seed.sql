INSERT INTO Products (Sku, Name, Price, IsActive)
SELECT v.Sku,
       v.Name,
       v.Price,
       v.IsActive
FROM   (VALUES ('KB-001', 'Mechanical Keyboard', 89.90, 1), ('MS-001', 'Wireless Mouse', 34.50, 1), ('HS-001', 'USB-C Headset', 59.00, 1), ('MN-001', '27-inch Monitor', 249.99, 1), ('CB-001', 'USB-C to HDMI Cable', 14.90, 1), ('WC-001', 'HD Webcam (discontinued)', 39.90, 0)) AS v(Sku, Name, Price, IsActive)
WHERE  NOT EXISTS (SELECT 1
                   FROM   Products AS p
                   WHERE  p.Sku = v.Sku);

SELECT   Sku,
         Name,
         Price,
         IsActive
FROM     Products
ORDER BY Sku;