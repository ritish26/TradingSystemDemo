-- =========================
-- OutboxMessages Table
-- =========================
CREATE TABLE "OutboxMessage" (
                                 "Id" TEXT PRIMARY KEY,
                                 "OrderId" TEXT NULL,
                                 "EventType" TEXT NOT NULL,
                                 "Payload" TEXT NOT NULL,
                                 "CreatedAt" TIMESTAMP NOT NULL,
                                 "ProcessedAt" TIMESTAMP NULL,
                                 "RetryCount" INTEGER NOT NULL DEFAULT 0,
                                 "Status" TEXT NOT NULL
);