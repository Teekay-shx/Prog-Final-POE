/* =========================================================
   RaceDay System - Database Schema and Seed Data
   Target: SQL Server (SSMS)
   This script matches the ERD in /docs/raceday_erd.png exactly.

   ADDENDUM (Part 2 back-port): Part 2's functional requirements
   specify that every Event must capture a Distance and an
   EventType (Run/Walk/Cycle), which were not present in the
   original Part 1 submission. These two columns (DistanceKm,
   EventType) were added to dbo.Events so the Part 1 plan stays
   in sync with the Part 2 implementation. No other table changed.
   ========================================================= */

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

/* Drop tables if they already exist, in FK-safe order, so the script
   can be re-run cleanly on a fresh instance. */
IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* =========================================================
   1. Users
   ========================================================= */
CREATE TABLE dbo.Users (
    UserID        INT IDENTITY(1,1) PRIMARY KEY,
    FullName      NVARCHAR(100) NOT NULL,
    Email         NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(255) NOT NULL,
    Role          NVARCHAR(20)  NOT NULL
                  CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin','Organiser','Participant')),
    Phone         NVARCHAR(20)  NULL,
    DateOfBirth   DATE          NULL,
    ProfilePictureUrl NVARCHAR(500) NULL,  -- Part 3: public Azure Blob URL
    CreatedAt     DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

/* =========================================================
   2. Events
   ========================================================= */
CREATE TABLE dbo.Events (
    EventID       INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID   INT NOT NULL,
    Name          NVARCHAR(150) NOT NULL,
    EventDate     DATE NOT NULL,
    Location      NVARCHAR(200) NULL,
    Description   NVARCHAR(MAX) NULL,
    DistanceKm    DECIMAL(5,2) NULL,
    EventType     NVARCHAR(10) NOT NULL DEFAULT 'Run'
                  CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run','Walk','Cycle')),
    Status        NVARCHAR(20) NOT NULL DEFAULT 'Planned'
                  CONSTRAINT CK_Events_Status CHECK (Status IN ('Planned','Open','Closed','Completed','Cancelled')),
    BannerImageUrl NVARCHAR(500) NULL,     -- Part 3: public Azure Blob URL
    CreatedAt     DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID)
);
GO

/* =========================================================
   3. Categories
   ========================================================= */
CREATE TABLE dbo.Categories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT NOT NULL,
    Name            NVARCHAR(100) NOT NULL,
    DistanceKm      DECIMAL(5,2) NULL,
    MinAge          INT NULL,
    MaxAge          INT NULL,
    EntryFee        DECIMAL(8,2) NOT NULL DEFAULT 0,
    MaxParticipants INT NULL,
    CONSTRAINT FK_Categories_Event FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);
GO

/* =========================================================
   4. Enrolments
   ========================================================= */
CREATE TABLE dbo.Enrolments (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT NOT NULL,
    CategoryID      INT NOT NULL,
    BibNumber       NVARCHAR(10) NULL,
    EnrolmentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Registered'
                    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Registered','CheckedIn','Cancelled')),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (CategoryID) REFERENCES dbo.Categories(CategoryID),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantID, CategoryID)
);
GO

/* =========================================================
   5. Results  (one-to-one with Enrolments)
   ========================================================= */
CREATE TABLE dbo.Results (
    ResultID     INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID  INT NOT NULL UNIQUE,
    FinishTime   TIME NULL,
    Position     INT NULL,
    Status       NVARCHAR(20) NOT NULL DEFAULT 'Finished'
                 CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished','DNF','DSQ')),
    RecordedAt   DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID)
);
GO

/* =========================================================
   6. Payments  (one-to-one with Enrolments)
   ========================================================= */
CREATE TABLE dbo.Payments (
    PaymentID      INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID    INT NOT NULL UNIQUE,
    Amount         DECIMAL(8,2) NOT NULL,
    PaymentMethod  NVARCHAR(30) NULL,
    PaymentDate    DATETIME NOT NULL DEFAULT GETDATE(),
    Status         NVARCHAR(20) NOT NULL DEFAULT 'Pending'
                   CONSTRAINT CK_Payments_Status CHECK (Status IN ('Pending','Completed','Refunded','Failed')),
    CONSTRAINT FK_Payments_Enrolment FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID)
);
GO

/* =========================================================
   SEED DATA
   ========================================================= */

-- Users: 2 Organisers, 2 Participants (plus 1 Admin for completeness)
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, Phone, DateOfBirth) VALUES
('Naledi Khumalo', 'naledi.khumalo@raceday.co.za', 'HASHED_PWD_1', 'Organiser', '0821234567', '1985-03-12'),
('Johan van der Merwe', 'johan.vdm@raceday.co.za', 'HASHED_PWD_2', 'Organiser', '0827654321', '1979-11-02'),
('Thabo Nkosi', 'thabo.nkosi@example.com', 'HASHED_PWD_3', 'Participant', '0731112222', '1997-06-21'),
('Lerato Dlamini', 'lerato.dlamini@example.com', 'HASHED_PWD_4', 'Participant', '0739998888', '2001-01-30'),
('Site Admin', 'admin@raceday.co.za', 'HASHED_PWD_5', 'Admin', NULL, NULL);
GO

-- Events: 3 events, owned by the two organisers
INSERT INTO dbo.Events (OrganiserID, Name, EventDate, Location, Description, DistanceKm, EventType, Status) VALUES
(1, 'Pretoria Spring Marathon', '2026-10-18', 'Union Buildings, Pretoria', 'Annual road race through the city centre.', 42.20, 'Run', 'Open'),
(1, 'Fun Run for Charity', '2026-11-08', 'Pretoria Botanical Garden', 'Family-friendly fun run supporting local charities.', 5.00, 'Walk', 'Planned'),
(2, 'Gauteng Trail Challenge', '2026-09-27', 'Magaliesberg Trail Park', 'Off-road trail running event across three distances.', 21.00, 'Run', 'Open');
GO

-- Categories: at least one per event
INSERT INTO dbo.Categories (EventID, Name, DistanceKm, MinAge, MaxAge, EntryFee, MaxParticipants) VALUES
(1, '42km Marathon', 42.20, 18, NULL, 350.00, 500),
(1, '21km Half Marathon', 21.10, 16, NULL, 250.00, 800),
(2, '5km Fun Run', 5.00, NULL, NULL, 100.00, 1000),
(3, '10km Trail', 10.00, 14, NULL, 150.00, 300),
(3, '21km Trail', 21.00, 18, NULL, 220.00, 200);
GO

-- Enrolments: sample sign-ups
INSERT INTO dbo.Enrolments (ParticipantID, CategoryID, BibNumber, Status) VALUES
(3, 1, 'A1001', 'Registered'),
(3, 4, 'A1002', 'Registered'),
(4, 2, 'A2001', 'CheckedIn'),
(4, 3, 'A2002', 'Registered');
GO

-- Results: sample finish for a checked-in enrolment
INSERT INTO dbo.Results (EnrolmentID, FinishTime, Position, Status) VALUES
(3, '01:45:32', 12, 'Finished');
GO

-- Payments: sample payment records
INSERT INTO dbo.Payments (EnrolmentID, Amount, PaymentMethod, Status) VALUES
(1, 350.00, 'Card', 'Completed'),
(2, 150.00, 'EFT', 'Completed'),
(3, 250.00, 'Card', 'Completed'),
(4, 100.00, 'Card', 'Pending');
GO
