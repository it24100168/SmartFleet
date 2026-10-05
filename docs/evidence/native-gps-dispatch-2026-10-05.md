# Native GPS capture and dispatch persistence

On 5 October 2026, the Android 16 Pixel 7 emulator granted SmartFleet **precise location while using the app**. The Flutter New Request form then displayed `GPS Captured: 37.42200, -122.08400` from the emulator's configured location; this is simulated emulator location, not a warehouse measurement. [Capture screen](mobile-gps-emulator-2026-10-05.png).

As Demo Operator, I submitted a **Standard**, **High** priority dispatch from `WarehouseA-DockA1` to `WarehouseA-DockB3` with the captured coordinates. Flutter My History showed request `d4a95301-3ec4-4e87-a82e-635e127b8891`, the GPS value, and finally **Completed**. [Completed request screen](mobile-gps-dispatch-2026-10-05.png). The authenticated API returned `latitude: 37.4219983`, `longitude: -122.084`, `status: Completed` and rover `44444444-4444-4444-4444-444444444444` from the local PostgreSQL-backed service.

This verifies one native Android permission/capture → Flutter request → ASP.NET Core API → persisted location path. It does not verify a physical device's GPS accuracy or hosted API integration. The user's real location is optional on the form and should not be required for the warehouse route simulation.
