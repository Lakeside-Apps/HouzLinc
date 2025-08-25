# HouzLinc Privacy Policy

**Last Updated:** August 25, 2025

## Overview

HouzLinc is a home automation application that helps you manage Insteon smart home devices on your local network. This privacy policy explains how we handle your information.

## What Data We Store

- **Device configurations**: Smart home device settings, scenes, and automation rules
- **Application settings**: Window preferences, app configuration options
- **No personal data**: We don't collect names, emails, locations, or usage analytics

## Where Your Data Lives

- **Locally**: Configuration stored in files on your device under your complete control
- **OneDrive (Optional)**: If enabled, your config file is stored in `Apps\HouzLinc` folder of your personal OneDrive
- **Your network only**: All device communication happens locally between the app and your Insteon Hub

## What We Don't Do

- ❌ No telemetry, analytics, or usage tracking
- ❌ No data collection for marketing or advertising  
- ❌ No transmission of data to our servers (we don't have servers)
- ❌ No access to other files or folders on your device or cloud storage

## OneDrive Integration (Optional)

- Completely optional cloud storage for configuration backup/sync
- Uses Microsoft Authentication Library with minimal scope: `Files.ReadWrite.AppFolder`
- Only accesses the dedicated `Apps\HouzLinc` folder in your OneDrive
- Authentication handled entirely by Microsoft's secure systems

## Open Source Transparency

- Full source code available for review: https://github.com/Lakeside-Apps/HouzLinc
- No hidden data collection or processing
- Community can verify privacy practices through code inspection

## Your Control

- **Complete ownership**: All configuration data belongs to you
- **Easy removal**: Uninstall the app to remove all local data, or manually delete the configuration file from the location you specified when setting up HouzLinc
- **OneDrive data removal**: Delete the `houselinc.xml` file from the `Apps\HouzLinc` folder in your OneDrive to remove cloud-stored data
- **Cloud independence**: App works fully offline without OneDrive
- **Data portability**: Configuration files can be backed up, moved, or shared as you choose

## Data Retention

- Local data is retained on your device until you uninstall the app or delete the configuration file from its storage location
- OneDrive data is retained in your personal OneDrive account under your control
- We do not retain any data on our servers (we don't have servers)

## Children's Privacy

HouzLinc is not directed at children under 13. We do not knowingly collect personal information from children under 13.

## Contact Information

For questions about this privacy policy:
- GitHub Issues: https://github.com/Lakeside-Apps/HouzLinc/issues
- Email: [info@lakesideapps.com](mailto:info@lakesideapps.com)

---

*This privacy policy reflects HouzLinc's commitment to user privacy and data protection. As an open source project, our practices are transparent and auditable by the community.*
