# Casambi_Gateway_API_Test

Small cmd test programm to show how to read data from an casambi Network

The Testprogramm connects to an Casambi Network (over the Gateway) via UDP. The Gateway must be set on "UDP Casambi Command".

The "Net ID" is used for connection more then one gateway. In the demo it is set to 0.
The "UDP-Port" is set to 10009.
These settings are the standard settings in the Casambi Gateway.

The Demo is asking the casambi Network for all devices (1 to 251) and if the device exists it will answer with:
- Unit_ID
- active Scene
- Priority Mode
- Node Type
- Condition of the Device
- online

More commands that can be send and the documentation of these commands is in the manual of the Casambi Gateway.

For more information visit: https://casambi.intelligent-lighting.de/.
