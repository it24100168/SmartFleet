# SmartFleet Mobile Client

The mobile companion application for Operators (dispatch and breakdown reports), Maintenance Technicians (rover diagnostics and repair), and Supervisors (fleet inspection and cross-client approval workflow). New users may self-register as Operators; privileged roles are assigned through the backend's seeded/provisioned accounts.

## Architecture
- **State & Routing**: `go_router` with reactive auth state redirect, `provider` for session management.
- **Secure Persistence**: `flutter_secure_storage` storing encrypted JWT tokens in Android Keystore / iOS Keychain.
- **Networking**: `http` package with dedicated `ApiClient` injecting `Authorization: Bearer <token>` automatically.

## Running Locally

Start the PostgreSQL-backed API first using the [assessment launcher](../scripts/start-assessment.ps1). The Android emulator reaches the host API through `10.0.2.2`.

```bash
# From the mobile directory
flutter pub get
flutter run -d emulator-5554 --dart-define=API_BASE_URL=http://10.0.2.2:5078/api
```

For a physical device, substitute the computer's reachable LAN address for `10.0.2.2`. The API address is supplied at launch rather than by editing source. Run `flutter test --no-pub` for the mobile tests.
