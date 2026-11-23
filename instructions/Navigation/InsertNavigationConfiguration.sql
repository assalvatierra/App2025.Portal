-- ============================================================================
-- Navigation Configuration Setup Script
-- ============================================================================
-- This script inserts the Navigation configuration into the PortalConfiguration table
-- The navigation menu items are stored as JSON in the Settings column
-- 
-- IsEnabled flag: 1 = Enabled (visible), 2 = Disabled (hidden)
-- ============================================================================

INSERT INTO [dbo].[PortalConfiguration] (Name, Description, IsEnabled, Expiry, SysCode, Settings)
VALUES (
	'Navigation',
	'Navigation menu items for the portal',
	1,
	'2099-12-31',
	'PORTAL',
	'[
		{"label":"Home","actionName":"Index","controllerName":"Home","isEnabled":1},
		{"label":"Our Fleet","actionName":"OurProducts","controllerName":"Home","isEnabled":1},
		{"label":"Car Rental Services","actionName":"OurServices","controllerName":"Home","isEnabled":1},
		{"label":"Car Rental Blogs","actionName":"Articles","controllerName":"Home","isEnabled":1},
		{"label":"FAQs","actionName":"FaqList","controllerName":"Home","isEnabled":1},
		{"label":"Privacy","actionName":"Privacy","controllerName":"Home","isEnabled":1},
		{"label":"Ask AI","actionName":"AskAI","controllerName":"Home","isEnabled":1},
		{"label":"VectorDB","actionName":"VectorDB","controllerName":"Home","isEnabled":1}
	]'
);

-- ============================================================================
-- Note: If you want to update existing navigation, use this query instead:
-- ============================================================================
-- UPDATE [dbo].[PortalConfiguration]
-- SET Settings = '[
--     {"label":"Home","actionName":"Index","controllerName":"Home","isEnabled":1},
--     {"label":"Our Fleet","actionName":"OurProducts","controllerName":"Home","isEnabled":1},
--     {"label":"Car Rental Services","actionName":"OurServices","controllerName":"Home","isEnabled":1},
--     {"label":"Car Rental Blogs","actionName":"Articles","controllerName":"Home","isEnabled":1},
--     {"label":"FAQs","actionName":"FaqList","controllerName":"Home","isEnabled":1},
--     {"label":"Privacy","actionName":"Privacy","controllerName":"Home","isEnabled":1},
--     {"label":"Ask AI","actionName":"AskAI","controllerName":"Home","isEnabled":1},
--     {"label":"VectorDB","actionName":"VectorDB","controllerName":"Home","isEnabled":2}
-- ]'
-- WHERE Name = 'Navigation' AND SysCode = 'PORTAL';

-- ============================================================================
-- Example: Disable a navigation item
-- ============================================================================
-- Update VectorDB to disabled (isEnabled = 2):
-- UPDATE [dbo].[PortalConfiguration]
-- SET Settings = (
--     SELECT REPLACE(Settings, '"isEnabled":1}', '"isEnabled":2}')
--     FROM [dbo].[PortalConfiguration]
--     WHERE Name = 'Navigation' AND SysCode = 'PORTAL'
-- )
-- WHERE Name = 'Navigation' AND SysCode = 'PORTAL';

-- ============================================================================
-- Verification Query - Run this to verify the navigation was inserted
-- ============================================================================
-- SELECT * FROM [dbo].[PortalConfiguration] WHERE Name = 'Navigation' AND SysCode = 'PORTAL';

