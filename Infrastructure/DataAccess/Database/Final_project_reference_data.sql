--
-- PostgreSQL database dump
--

--\restrict tCF15uVqIkagzuVpeb8LWrYrvMErKf9EYKflbLj7fOogkhzDqy61HphwgfG9gpT

-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Data for Name: commandcatalogue; Type: TABLE DATA; Schema: public; Owner: postgres
--
INSERT INTO public.commandcatalogue
    (id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate)
VALUES
    (1,  'AUTO',                'Let the adapter select the local autonomous mode.',              'Mode',       'None',       NULL,            false, true, NOW(), NOW()),
    (2,  'STOP',                'Stop current device movement or operation.',                     'Mode',       'None',       NULL,            false, true, NOW(), NOW()),
    (3,  'ROTATE_LEFT',         'Rotate the device left using adapter-specific behavior.',        'Continuous', 'BestEffort', 'ROTATE_RIGHT',  true,  true, NOW(), NOW()),
    (4,  'ROTATE_RIGHT',        'Rotate the device right using adapter-specific behavior.',       'Continuous', 'BestEffort', 'ROTATE_LEFT',   true,  true, NOW(), NOW()),
    (5,  'MOVE_FORWARD',        'Move forward using adapter-specific behavior.',                  'Continuous', 'BestEffort', 'MOVE_BACKWARD', true,  true, NOW(), NOW()),
    (6,  'MOVE_BACKWARD',       'Move backward using adapter-specific behavior.',                 'Continuous', 'BestEffort', 'MOVE_FORWARD',  true,  true, NOW(), NOW()),
    (7,  'RETURN_TO_SAFE_ZONE', 'Return to a safe area using adapter-local navigation.',          'Mode',       'None',       NULL,            false, true, NOW(), NOW()),
    (10, 'REPORT',              'Report device status or telemetry.',                            'Query',      'None',       NULL,            false, true, NOW(), NOW()),
    (11, 'DRIVE_DISTANCE',      'Drive a supplied distance in centimeters or adapter-defined units.', 'Grid',   'Exact',      'DRIVE_DISTANCE', false, true, NOW(), NOW()),
    (12, 'ROTATE_DEGREES',      'Rotate a supplied number of degrees.',                          'Grid',       'Exact',      'ROTATE_DEGREES', false, true, NOW(), NOW()),
    (13, 'PLACE',               'Set or reset logical grid pose.',                               'Mode',       'None',       NULL,            false, true, NOW(), NOW()),
    (14, 'MOVE',                'Move one grid cell forward when grid pose is trusted.',          'Grid',       'Exact',      'STEP_BACK',      false, true, NOW(), NOW()),
    (15, 'LEFT',                'Rotate left on a logical grid.',                                'Grid',       'Exact',      'RIGHT',          false, true, NOW(), NOW()),
    (16, 'RIGHT',               'Rotate right on a logical grid.',                               'Grid',       'Exact',      'LEFT',           false, true, NOW(), NOW()),
    (17, 'STEP_BACK',           'Move one grid cell backward when grid pose is trusted.',         'Grid',       'Exact',      'MOVE',           false, true, NOW(), NOW()),
    (18, 'JUMP_FORWARD',        'Legacy grid command: jump forward multiple cells.',              'Grid',       'Exact',      'JUMP_BACKWARD',  false, true, NOW(), NOW()),
    (19, 'JUMP_BACKWARD',       'Legacy grid command: jump backward multiple cells.',             'Grid',       'Exact',      'JUMP_FORWARD',   false, true, NOW(), NOW());
INSERT INTO public.map
    (id, name, columns, rows, cellsizecm, description, isactive, createddate, modifieddate)
OVERRIDING SYSTEM VALUE
VALUES
    (1, 'Default Grid Map', 10, 10, 30, 'Default seeded grid map for seeded devices.', true, NOW(), NOW())
ON CONFLICT (id) DO UPDATE
SET
    name = EXCLUDED.name,
    columns = EXCLUDED.columns,
    rows = EXCLUDED.rows,
    cellsizecm = EXCLUDED.cellsizecm,
    description = EXCLUDED.description,
    isactive = EXCLUDED.isactive,
    modifieddate = NOW();
-- Data for Name: device; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public.device (id, name, deviceidentifier, devicetype, mapid, description, isactive, createddate, modifieddate) VALUES (1, 'Smart Mobile Clothesline Robot', 'clothesline_robot_01', 'TwoWheelDriveCar', 1, 'Arduino Nano 33 IoT smart mobile clothesline robocar. Hardware behavior stays in the adapter/Nano layer.', true, '2026-05-13 01:05:14.513354', '2026-05-13 01:05:14.513354');
INSERT INTO public.device (id, name, deviceidentifier, devicetype, mapid, description, isactive, createddate, modifieddate) VALUES (2, 'Legacy Grid Robot', 'legacy_grid_robot_01', 'LegacyGridRobot', 1, 'Development seed for the legacy grid robot.', true, '2026-05-13 01:11:45.282195', '2026-05-13 01:11:45.282195');


--
-- Data for Name: devicecapability; Type: TABLE DATA; Schema: public; Owner: postgres
--

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
        -- Live / physical robot capabilities
        (1,  'AUTO',                false, 'Live command: allow autonomous local clothesline behavior.'),
        (2,  'STOP',                false, 'Live command: stop motors or current adapter operation.'),
        (3,  'ROTATE_LEFT',         false, 'Live command: rotate toward the left side.'),
        (4,  'ROTATE_RIGHT',        false, 'Live command: rotate toward the right side.'),
        (5,  'MOVE_FORWARD',        false, 'Live command: move forward using local adapter semantics.'),
        (6,  'MOVE_BACKWARD',       false, 'Live command: move backward using local adapter semantics.'),
        (7,  'RETURN_TO_SAFE_ZONE', false, 'Live command: move toward safe/shaded zone using local autonomy.'),
        (8,  'REPORT',              false, 'Live command: report status or telemetry.'),
        (9,  'DRIVE_DISTANCE',      false, 'Live command: drive a supplied distance.'),
        (10, 'ROTATE_DEGREES',      false, 'Live command: rotate a supplied number of degrees.'),

        -- Grid capabilities for Device 1
        (11, 'PLACE',               true,  'Grid command: set or reset logical grid pose.'),
        (12, 'MOVE',                true,  'Grid command: move one grid cell forward.'),
        (13, 'LEFT',                true,  'Grid command: rotate left on the logical grid.'),
        (14, 'RIGHT',               true,  'Grid command: rotate right on the logical grid.'),
        (15, 'STEP_BACK',           true,  'Grid command: move one grid cell backward.')
) AS v(id, commandname, requiresmap, description)
JOIN public.commandcatalogue cc
    ON cc.name = v.commandname;
--
-- Data for Name: devicestatus; Type: TABLE DATA; Schema: public; Owner: postgres
--

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
        0,
        0,
        'North',
        true,
        true,
        1.0,
        NULL,
        NULL,
        NULL,
        true,
        'Seeded clothesline robot with assigned map and trusted grid pose.',
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
        0,
        0,
        'North',
        true,
        true,
        1.0,
        NULL,
        NULL,
        NULL,
        true,
        'Seeded legacy robot with assigned map and trusted grid pose.',
        NULL,
        NULL,
        NOW(),
        NOW()
    );

--
-- Name: commandcatalogue_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.commandcatalogue_id_seq', 19, true);


--
-- Name: device_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.device_id_seq', 2, true);


--
-- Name: devicecapability_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.devicecapability_id_seq', 20, true);


--
-- Name: devicestatus_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.devicestatus_id_seq', 2, true);


--
-- PostgreSQL database dump complete
--

--\unrestrict tCF15uVqIkagzuVpeb8LWrYrvMErKf9EYKflbLj7fOogkhzDqy61HphwgfG9gpT


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
