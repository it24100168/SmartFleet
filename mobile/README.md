# SmartFleet Mobile Client

The mobile companion application for Factory Operators (initiating dispatch missions and reporting breakdown emergencies) and Maintenance Technicians (accessing rover diagnostics on the warehouse floor).

## Architecture
- **State & Routing**: `go_router` with reactive auth state redirect, `provider` for session management.
- **Secure Persistence**: `flutter_secure_storage` storing encrypted JWT tokens in Android Keystore / iOS Keychain.
- **Networking**: `http` package with dedicated `ApiClient` injecting `Authorization: Bearer <token>` automatically.

## Running Locally
```bash
# 1. Fetch dependencies
flutter pub get

# 2. Run on emulator, physical device, or desktop/web
flutter run
```

Configure `lib/core/constants/api_constants.dart` with your machine IP or `10.0.2.2` for Android emulator.
