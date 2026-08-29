-- Creates bd only. The schemes is being applied by migrations
-- of EF Core on service startup (db.Database.Migrate() in Program.cs).
CREATE DATABASE catalog_db;
CREATE DATABASE orders_db;
CREATE DATABASE inventory_db;
CREATE DATABASE order_saga_db;