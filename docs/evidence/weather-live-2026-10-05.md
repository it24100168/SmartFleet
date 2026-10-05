# Third-party weather evidence — 5 October 2026

The backend's keyless Open-Meteo integration uses fixed Colombo coordinates (`6.9271, 79.8612`) rather than user, rover or mission data. At approximately **11:30 UTC**, a read-only request to Open-Meteo's current forecast endpoint returned observation time `2026-10-05T11:30`, temperature `28.6 °C`, precipitation `0.10 mm`, and WMO weather code `51`. The tested mapping classifies code 51 as **medium** risk; the dispatch/guard workflow permits low or medium risk subject to its other checks, while high or unknown risk is blocked. This request was made from the development workstation and does **not** prove Azure-hosted network access yet.

Automated `WeatherServiceTests` verify low/medium/high/unknown mapping, a real-provider output flag, no mission/zone string in the external request URL, invalid response fail-closed behavior, retries, OpenWeather error behavior, and caller cancellation. The PostgreSQL-backed backend suite finished **61 passed, 0 skipped** on this working tree. The final Git commit/CI run must be recorded after pushing.

The service is [Open-Meteo](https://open-meteo.com/), credited under CC BY 4.0. Its [non-commercial Free API terms](https://open-meteo.com/en/terms) apply to this educational demonstration. Weather is an external regional proxy for the indoor/outdoor dock scenario, not a physical warehouse sensor.
