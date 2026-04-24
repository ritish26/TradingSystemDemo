-- Enable UUID generation (modern)
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =========================
-- Orders Table
-- =========================
CREATE TABLE "Orders" (
                          "OrderId" TEXT PRIMARY KEY,
                          "ClientId" TEXT NULL,
                          "InstrumentId" TEXT NULL,
                          "OrderType" INTEGER NOT NULL,
                          "Quantity" INTEGER NOT NULL,
                          "Price" NUMERIC(18,2) NOT NULL,
                          "CreatedAt" TIMESTAMP NOT NULL,
                          "Status" TEXT NOT NULL
);