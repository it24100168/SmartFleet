# ADR 002: Client state and refresh

Status: documents current implementation, 27 September 2026.

React uses Context for authentication and component state for forms, tables and polling. Flutter uses Provider/ChangeNotifier for shared authentication and breakdown services, with local StatefulWidget state for dispatch and fleet screens. This avoids a new state framework for the current small application. Alternatives such as Redux or Riverpod would add migration work without removing the need for server-side authorization.

The backend owns mission status, reservations and decisions. Clients refresh persisted state; they do not advance missions. Costs include duplicated refresh/error handling and no offline conflict resolution. React currently stores its bearer token in localStorage, which is exposed to successful script injection; Flutter uses secure storage on supported native platforms. UI role checks improve navigation but are never authorization boundaries.
