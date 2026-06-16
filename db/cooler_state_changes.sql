-- Setup for the fan/cooler on-off history shown as an overlay on the temperature graph.
-- Run this once against the same MySQL/MariaDB database the AquariumController and InfoPages use.

CREATE TABLE IF NOT EXISTS cooler_state_changes (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    channel     VARCHAR(20) NOT NULL,   -- 'fan' or 'cooler'
    state       TINYINT(1) NOT NULL,    -- 1 = on, 0 = off
    created_at  TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
