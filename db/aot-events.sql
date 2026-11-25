-- Run once on the Pi before starting the new version:
--   mysql -u <user> -p <database> < aot-events.sql

-- Events shown on the graphs, fx. when the AOT system has added new water
CREATE TABLE IF NOT EXISTS events (
    id INT NOT NULL AUTO_INCREMENT,
    type VARCHAR(50) NOT NULL,
    value DOUBLE NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_events_created_at (created_at)
);

-- Temperature change (in C) within AotWindowMinutes that counts as an AOT run
INSERT INTO settings (title, value)
SELECT 'AotTempDelta', '0.2' FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM settings WHERE title = 'AotTempDelta');

INSERT INTO settings (title, value)
SELECT 'AotWindowMinutes', '2' FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM settings WHERE title = 'AotWindowMinutes');
