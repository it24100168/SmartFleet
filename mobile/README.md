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

## Hosted assessment APK

Build against the same public API used by React:

```powershell
cd mobile
C:\flutter\bin\flutter.bat build apk --release --dart-define=API_BASE_URL=https://smartfleet-api-a0gmajhpg2ercjch.eastasia-01.azurewebsites.net/api
& 'C:\Users\hamdh\AppData\Local\Android\Sdk\platform-tools\adb.exe' install -r build\app\outputs\flutter-apk\app-release.apk
```

The 5 October assessment build was copied to ignored `.demo/SE3090_G02.apk` at the repository root (SHA-256 `41D85A181D76A242CDA656C9B35BBB4BCD7B81C3C7E1A6D75842D98210CE8C7D`). Supply that APK separately with the submission; it is not in Git. It installed and launched on the Pixel 7 Android emulator, and the team subsequently registered an Operator against the hosted API. The login form's demo-prefilled credentials are for local simulation only; use a new Operator account or the privately provisioned hosted account. The APK uses the Android development signing key and is not a store-signed release.
