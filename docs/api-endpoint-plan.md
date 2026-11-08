# RaceDay API Endpoint Plan

Roles: **Public** (no login), **Any** (any logged-in user), **Participant**, **Organiser**, **Admin**.

> **Addendum (Part 2 back-port):** Part 2's functional requirements require every Event to capture
> a `distance` and `type` (Run/Walk/Cycle), which were not in the original Part 1 ERD. These two
> fields have been added to the Events request/response bodies below, and to `raceday_erd.png` and
> `raceday_schema.sql`, so the plan stays in sync with the Part 2 implementation. See the Part 2
> README for the full explanation.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as a Participant or Organiser. | Public | `{ "fullName", "email", "password", "role", "phone", "dateOfBirth" }` | 201 Created — user object (no password) + JWT token. 400 Bad Request — validation error. 409 Conflict — email already registered. |
| POST | /api/auth/login | Authenticates a user and returns a session token. | Public | `{ "email", "password" }` | 200 OK — user object + JWT token. 401 Unauthorized — invalid credentials. |
| POST | /api/auth/logout | Invalidates the current session/token. | Any | None | 200 OK — logout confirmed. |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently logged-in user. | Any | None | 200 OK — user profile object. 401 Unauthorized. |
| PUT | /api/users/me | Updates the logged-in user's own profile details. | Any | `{ "fullName", "phone", "dateOfBirth" }` | 200 OK — updated profile. 400 Bad Request — validation error. |
| POST | /api/users/me/profile-picture | Uploads/replaces the profile picture (stored in Azure Blob Storage by the API). | Any | multipart/form-data `file` (JPEG/PNG/WebP/GIF, max 5 MB) | 200 OK — profile with `profilePictureUrl`. 400 Bad Request — invalid file. 503 — storage not configured. |
| GET | /api/users/{id} | Returns a specific user's public profile (e.g. an organiser's public page). | Admin | None | 200 OK — user object. 404 Not Found. |
| GET | /api/users | Lists all users, with optional role filter, for admin management. | Admin | None | 200 OK — array of user objects. |
| DELETE | /api/users/{id} | Deactivates/removes a user account. | Admin | None | 204 No Content. 404 Not Found. |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all events, with optional filters (status, type, date, location). | Public | None | 200 OK — array of event objects. |
| GET | /api/events/mine | Lists the organiser's own events with enrolment and category totals (dashboard). | Organiser | None | 200 OK — array of organiser event objects. |
| GET | /api/events/{id} | Returns full details for a single event, including its categories. | Public | None | 200 OK — event object with nested categories. 404 Not Found. |
| POST | /api/events | Creates a new event owned by the logged-in organiser. | Organiser | `{ "name", "eventDate", "location", "description", "distance", "type" }` | 201 Created — event object. 400 Bad Request — validation error. |
| PUT | /api/events/{id} | Updates an event's details. Only the owning organiser (or Admin) may edit. | Organiser | `{ "name", "eventDate", "location", "description", "distance", "type", "status" }` | 200 OK — updated event. 403 Forbidden — not the event owner. 404 Not Found. |
| POST | /api/events/{id}/banner | Uploads the event banner (stored in Azure Blob Storage by the API). | Organiser | multipart/form-data `file` (max 5 MB image) | 200 OK — event with `bannerImageUrl`. 400, 403 Forbidden, 404, 503. |
| DELETE | /api/events/{id} | Cancels/removes an event. | Organiser | None | 204 No Content. 403 Forbidden. 404 Not Found. |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all categories belonging to an event. | Public | None | 200 OK — array of category objects. 404 Not Found — event does not exist. |
| POST | /api/events/{eventId}/categories | Adds a new category (e.g. "10km") to an event. | Organiser | `{ "name", "distanceKm", "minAge", "maxAge", "entryFee", "maxParticipants" }` | 201 Created — category object. 403 Forbidden — not the event owner. |
| PUT | /api/categories/{id} | Updates a category's details. | Organiser | `{ "name", "distanceKm", "minAge", "maxAge", "entryFee", "maxParticipants" }` | 200 OK — updated category. 403 Forbidden. 404 Not Found. |
| DELETE | /api/categories/{id} | Removes a category from an event (only if it has no enrolments). | Organiser | None | 204 No Content. 403 Forbidden. 409 Conflict — category has active enrolments. |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{categoryId}/enrolments | Enrols the logged-in participant into a category of an event. | Participant | `{ }` (participant is inferred from the token) | 201 Created — enrolment record with bib number. 404 Not Found — category does not exist. 409 Conflict — already enrolled or category full. |
| GET | /api/users/me/enrolments | Lists the logged-in participant's own enrolments across all events. | Participant | None | 200 OK — array of enrolment objects. |
| GET | /api/events/{eventId}/enrolments | Lists every enrolment for an event across all categories, with any captured result. | Organiser | None | 200 OK — array of enrolment objects. 403 Forbidden — not the event owner. 404 Not Found. |
| GET | /api/categories/{categoryId}/enrolments | Lists all participants enrolled in a category, for the organiser's roster. | Organiser | None | 200 OK — array of enrolment objects. 403 Forbidden — not the event owner. |
| PUT | /api/enrolments/{id} | Updates enrolment status (e.g. check-in on race day). | Organiser | `{ "status" }` | 200 OK — updated enrolment. 403 Forbidden. 404 Not Found. |
| DELETE | /api/enrolments/{id} | Cancels an enrolment. | Participant | None | 204 No Content. 403 Forbidden — not the enrolment owner. 404 Not Found. |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/categories/{categoryId}/results | Returns the results leaderboard for a category, ordered by position. | Public | None | 200 OK — array of result objects. 404 Not Found — category does not exist. |
| GET | /api/users/me/results | Returns the logged-in participant's own race results, with event, category and total finishers ("47th out of 312"). | Participant | None | 200 OK — array of result objects. |
| POST | /api/enrolments/{enrolmentId}/results | Records a finishing result for a participant's enrolment. | Organiser | `{ "finishTime", "position", "status" }` | 201 Created — result object. 403 Forbidden — not the event owner. 409 Conflict — result already recorded for this enrolment. |
| PUT | /api/results/{id} | Corrects/updates a previously recorded result. | Organiser | `{ "finishTime", "position", "status" }` | 200 OK — updated result. 403 Forbidden. 404 Not Found. |

## Payments *(supporting entity, not explicitly required but needed to support paid enrolments)*

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/payments | Records a payment for an enrolment's entry fee. | Participant | `{ "amount", "paymentMethod" }` | 201 Created — payment object. 404 Not Found — enrolment does not exist. |
| GET | /api/enrolments/{enrolmentId}/payments | Returns the payment record for an enrolment. | Any (owner or Organiser) | None | 200 OK — payment object. 403 Forbidden. 404 Not Found. |
