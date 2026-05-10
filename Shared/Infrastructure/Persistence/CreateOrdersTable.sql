-- Enable UUID generation (modern)
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =========================
-- Orders Table
-- =========================
-- Optimistic concurrency control: Uses PostgreSQL's built-in xmin system column.
-- xmin is the internal transaction ID that changes on every UPDATE, preventing lost updates.
-- No explicit column needed - xmin exists on all PostgreSQL rows automatically.
CREATE TABLE IF NOT EXISTS "Orders" (
    "OrderId" TEXT PRIMARY KEY,
    "ClientId" TEXT NULL,
    "InstrumentId" TEXT NULL,
    "OrderType" INTEGER NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "Price" NUMERIC(18,2) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "Status" TEXT NOT NULL
    -- xmin (system column) is used as the concurrency token - no explicit column needed
);
