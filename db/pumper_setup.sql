-- Setup for the "Pumper" Hue smart-plug monitoring feature.
-- Run this once against the same MySQL database the AquariumController and InfoPages use.

-- Table that stores every on/off state change of the Pumper smart plug.
CREATE TABLE IF NOT EXISTS pumper_state_changes (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    from_state  TINYINT(1) NOT NULL,
    to_state    TINYINT(1) NOT NULL,
    created_at  TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Setting that holds the Hue name of the smart plug to watch.
-- It shows up automatically on the Settings page, where you can edit it.
-- Change 'Pumper' below if your plug has a different name in the Hue app.
INSERT INTO settings (title, value)
SELECT 'PumperName', 'Pumper' FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM settings WHERE title = 'PumperName');
