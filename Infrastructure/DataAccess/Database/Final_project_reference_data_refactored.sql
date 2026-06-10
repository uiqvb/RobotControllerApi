-- Refactored reference data for Minimal Nano Robocar + Legacy Grid Robot.
-- Assumes Final_project_schema_refactored.sql has already been applied.

SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

-- =====================================================================
-- Command catalogue
--
-- Removed globally:
--   AUTO, RETURN_TO_SAFE_ZONE, DRIVE_DISTANCE, ROTATE_DEGREES
--
-- Nano commands:
--   Live/free-hand: STOP, MOVE_FORWARD, MOVE_BACKWARD, ROTATE_LEFT, ROTATE_RIGHT
--   Grid/workflow: PLACE, MOVE, LEFT, RIGHT, STEP_BACK
--
-- Legacy commands:
--   PLACE, MOVE, LEFT, RIGHT, STEP_BACK, JUMP_FORWARD, JUMP_BACKWARD, REPORT
-- =====================================================================

INSERT INTO public.commandcatalogue
    (id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate)
VALUES
    (2,  'STOP',          'Stop current device movement or operation.',                     'Mode',       'None',       NULL,            false, true, NOW(), NOW()),
    (3,  'ROTATE_LEFT',   'Live-control command: rotate the Nano robot left.',               'Continuous', 'BestEffort', 'ROTATE_RIGHT',  true,  true, NOW(), NOW()),
    (4,  'ROTATE_RIGHT',  'Live-control command: rotate the Nano robot right.',              'Continuous', 'BestEffort', 'ROTATE_LEFT',   true,  true, NOW(), NOW()),
    (5,  'MOVE_FORWARD',  'Live-control command: move the Nano robot forward.',              'Continuous', 'BestEffort', 'MOVE_BACKWARD', true,  true, NOW(), NOW()),
    (6,  'MOVE_BACKWARD', 'Live-control command: move the Nano robot backward.',             'Continuous', 'BestEffort', 'MOVE_FORWARD',  true,  true, NOW(), NOW()),
    (10, 'REPORT',        'Legacy query command: report logical grid position.',             'Query',      'None',       NULL,            false, true, NOW(), NOW()),
    (13, 'PLACE',         'Set or reset logical grid pose.',                                 'Mode',       'None',       NULL,            false, true, NOW(), NOW()),
    (14, 'MOVE',          'Move one grid cell forward when grid pose is trusted.',            'Grid',       'Exact',      'STEP_BACK',      false, true, NOW(), NOW()),
    (15, 'LEFT',          'Rotate left on a logical grid.',                                  'Grid',       'Exact',      'RIGHT',          false, true, NOW(), NOW()),
    (16, 'RIGHT',         'Rotate right on a logical grid.',                                 'Grid',       'Exact',      'LEFT',           false, true, NOW(), NOW()),
    (17, 'STEP_BACK',     'Move one grid cell backward when grid pose is trusted.',           'Grid',       'Exact',      'MOVE',           false, true, NOW(), NOW()),
    (18, 'JUMP_FORWARD',  'Legacy grid command: jump forward multiple cells.',                'Grid',       'Exact',      'JUMP_BACKWARD',  false, true, NOW(), NOW()),
    (19, 'JUMP_BACKWARD', 'Legacy grid command: jump backward multiple cells.',               'Grid',       'Exact',      'JUMP_FORWARD',   false, true, NOW(), NOW());

-- =====================================================================
-- Map
-- =====================================================================

INSERT INTO public.map
    (id, name, columns, rows, cellsizecm, description, isactive, createddate, modifieddate)
VALUES
    (1, 'Default Grid Map', 10, 10, 30, 'Default seeded 10x10 grid map for Nano and legacy devices.', true, NOW(), NOW());

-- =====================================================================
-- Devices
-- =====================================================================

INSERT INTO public.device
    (id, name, deviceidentifier, devicetype, mapid, description, isactive, createddate, modifieddate)
VALUES
    (1, 'Minimal Nano Robocar', 'nano_robocar_01', 'TwoWheelDriveCar', 1,
     'Arduino Nano 33 IoT two-wheel robot controlled by live commands, queued jobs, and workflows.',
     true, NOW(), NOW()),
    (2, 'Legacy Grid Robot', 'legacy_grid_robot_01', 'LegacyGridRobot', 1,
     'Legacy grid robot adapter for command sets, command executions, reports, jumps, and rollback demonstrations.',
     true, NOW(), NOW());

-- =====================================================================
-- Device capabilities
-- =====================================================================

-- Device 1: Minimal Nano Robocar.
-- Live/free-hand commands are exposed for live controls. Grid commands are exposed for queued jobs/workflows.
INSERT INTO public.devicecapability
    (id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate)
SELECT
    v.id,
    1 AS deviceid,
    cc.id AS commandcatalogueid,
    v.requiresmap,
    v.description,
    true AS isactive,
    NOW() AS createddate,
    NOW() AS modifieddate
FROM (
    VALUES
        (1,  'STOP',          false, 'Nano live/free-hand command: stop motors.'),
        (2,  'ROTATE_LEFT',   false, 'Nano live/free-hand command: rotate left while live control is fresh.'),
        (3,  'ROTATE_RIGHT',  false, 'Nano live/free-hand command: rotate right while live control is fresh.'),
        (4,  'MOVE_FORWARD',  false, 'Nano live/free-hand command: move forward while live control is fresh.'),
        (5,  'MOVE_BACKWARD', false, 'Nano live/free-hand command: move backward while live control is fresh.'),
        (6,  'PLACE',         true,  'Nano grid command: set/reset trusted logical grid pose; no motor movement.'),
        (7,  'MOVE',          true,  'Nano grid command: short calibrated forward pulse.'),
        (8,  'LEFT',          true,  'Nano grid command: short calibrated left rotation pulse.'),
        (9,  'RIGHT',         true,  'Nano grid command: short calibrated right rotation pulse.'),
        (10, 'STEP_BACK',     true,  'Nano grid command: short calibrated backward pulse.')
) AS v(id, commandname, requiresmap, description)
JOIN public.commandcatalogue cc
    ON cc.name = v.commandname;

-- Device 2: Legacy Grid Robot.
-- No live/free-hand motor commands. REPORT and jump commands are legacy-only capabilities.
INSERT INTO public.devicecapability
    (id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate)
SELECT
    v.id,
    2 AS deviceid,
    cc.id AS commandcatalogueid,
    v.requiresmap,
    v.description,
    true AS isactive,
    NOW() AS createddate,
    NOW() AS modifieddate
FROM (
    VALUES
        (11, 'PLACE',         true,  'Legacy grid command: place robot at a logical grid pose.'),
        (12, 'MOVE',          true,  'Legacy grid command: move one cell forward.'),
        (13, 'LEFT',          true,  'Legacy grid command: rotate left.'),
        (14, 'RIGHT',         true,  'Legacy grid command: rotate right.'),
        (15, 'STEP_BACK',     true,  'Legacy grid command: move one cell backward.'),
        (16, 'JUMP_FORWARD',  true,  'Legacy grid command: jump forward multiple cells.'),
        (17, 'JUMP_BACKWARD', true,  'Legacy grid command: jump backward multiple cells.'),
        (18, 'REPORT',        false, 'Legacy query command: report current logical pose or unplaced state.')
) AS v(id, commandname, requiresmap, description)
JOIN public.commandcatalogue cc
    ON cc.name = v.commandname;

-- =====================================================================
-- Device status
--
-- Both seeded devices start untrusted. PLACE is required before trusted
-- grid workflows should be accepted.
-- =====================================================================

INSERT INTO public.devicestatus
    (
        id,
        deviceid,
        connectionstate,
        operationalstate,
        lastseenatutc,
        lastheartbeatatutc,
        posemapid,
        gridx,
        gridy,
        facing,
        isgridaligned,
        isgridposetrusted,
        poseconfidence,
        estimatedxcm,
        estimatedycm,
        estimatedheadingdegrees,
        isinsidemap,
        statusmessage,
        lasterrorcode,
        lasterrormessage,
        createddate,
        modifieddate
    )
VALUES
    (
        1,
        1,
        'Unknown',
        'Idle',
        NULL,
        NULL,
        1,
        NULL,
        NULL,
        NULL,
        false,
        false,
        0.0,
        NULL,
        NULL,
        NULL,
        NULL,
        'Minimal Nano Robocar requires PLACE before trusted grid jobs or workflows.',
        NULL,
        NULL,
        NOW(),
        NOW()
    ),
    (
        2,
        2,
        'Unknown',
        'Idle',
        NULL,
        NULL,
        1,
        NULL,
        NULL,
        NULL,
        false,
        false,
        0.0,
        NULL,
        NULL,
        NULL,
        NULL,
        'Legacy Grid Robot requires PLACE before trusted grid workflows.',
        NULL,
        NULL,
        NOW(),
        NOW()
    );

-- =====================================================================
-- Sequence reset
-- =====================================================================

SELECT setval(pg_get_serial_sequence('public.commandcatalogue', 'id'), COALESCE((SELECT MAX(id) FROM public.commandcatalogue), 1), true);
SELECT setval(pg_get_serial_sequence('public.map', 'id'), COALESCE((SELECT MAX(id) FROM public.map), 1), true);
SELECT setval(pg_get_serial_sequence('public.device', 'id'), COALESCE((SELECT MAX(id) FROM public.device), 1), true);
SELECT setval(pg_get_serial_sequence('public.devicecapability', 'id'), COALESCE((SELECT MAX(id) FROM public.devicecapability), 1), true);
SELECT setval(pg_get_serial_sequence('public.devicestatus', 'id'), COALESCE((SELECT MAX(id) FROM public.devicestatus), 1), true);
SELECT setval(pg_get_serial_sequence('public.devicecredential', 'id'), COALESCE((SELECT MAX(id) FROM public.devicecredential), 1), true);
SELECT setval(pg_get_serial_sequence('public.appuser', 'id'), COALESCE((SELECT MAX(id) FROM public.appuser), 1), true);
SELECT setval(pg_get_serial_sequence('public.appusercredential', 'id'), COALESCE((SELECT MAX(id) FROM public.appusercredential), 1), true);
SELECT setval(pg_get_serial_sequence('public.devicepermission', 'id'), COALESCE((SELECT MAX(id) FROM public.devicepermission), 1), true);
SELECT setval(pg_get_serial_sequence('public.job', 'id'), COALESCE((SELECT MAX(id) FROM public.job), 1), true);
SELECT setval(pg_get_serial_sequence('public.workflow', 'id'), COALESCE((SELECT MAX(id) FROM public.workflow), 1), true);
SELECT setval(pg_get_serial_sequence('public.jobhistory', 'id'), COALESCE((SELECT MAX(id) FROM public.jobhistory), 1), true);
SELECT setval(pg_get_serial_sequence('public.workflowhistory', 'id'), COALESCE((SELECT MAX(id) FROM public.workflowhistory), 1), true);
SELECT setval(pg_get_serial_sequence('public.rollbackrequest', 'id'), COALESCE((SELECT MAX(id) FROM public.rollbackrequest), 1), true);
SELECT setval(pg_get_serial_sequence('public.telemetryreading', 'id'), COALESCE((SELECT MAX(id) FROM public.telemetryreading), 1), true);
SELECT setval(pg_get_serial_sequence('public.livecontrolcommand', 'id'), COALESCE((SELECT MAX(id) FROM public.livecontrolcommand), 1), true);
SELECT setval(pg_get_serial_sequence('public.livecontrolsession', 'id'), COALESCE((SELECT MAX(id) FROM public.livecontrolsession), 1), true);
SELECT setval(pg_get_serial_sequence('public.livecontrolsegment', 'id'), COALESCE((SELECT MAX(id) FROM public.livecontrolsegment), 1), true);
