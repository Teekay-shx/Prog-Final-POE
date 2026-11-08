using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Seeds the same sample data as the Part 1 SQL script (2 Organisers, 2 Participants,
/// 3 Events, categories per event, sample enrolments) so the API has data to exercise
/// through Swagger straight after the first migration. Runs once, only if Users is empty.
/// </summary>
public static class DbSeeder
{
    public static void Seed(RaceDayDbContext context, IPasswordHasher<User> hasher)
    {
        if (context.Users.Any())
        {
            return;
        }

        var organiser1 = new User
        {
            FullName = "Naledi Khumalo",
            Email = "naledi.khumalo@raceday.co.za",
            Role = UserRole.Organiser,
            Phone = "0821234567",
            DateOfBirth = new DateOnly(1985, 3, 12)
        };
        organiser1.PasswordHash = hasher.HashPassword(organiser1, "Password123!");

        var organiser2 = new User
        {
            FullName = "Johan van der Merwe",
            Email = "johan.vdm@raceday.co.za",
            Role = UserRole.Organiser,
            Phone = "0827654321",
            DateOfBirth = new DateOnly(1979, 11, 2)
        };
        organiser2.PasswordHash = hasher.HashPassword(organiser2, "Password123!");

        var participant1 = new User
        {
            FullName = "Thabo Nkosi",
            Email = "thabo.nkosi@example.com",
            Role = UserRole.Participant,
            Phone = "0731112222",
            DateOfBirth = new DateOnly(1997, 6, 21)
        };
        participant1.PasswordHash = hasher.HashPassword(participant1, "Password123!");

        var participant2 = new User
        {
            FullName = "Lerato Dlamini",
            Email = "lerato.dlamini@example.com",
            Role = UserRole.Participant,
            Phone = "0739998888",
            DateOfBirth = new DateOnly(2001, 1, 30)
        };
        participant2.PasswordHash = hasher.HashPassword(participant2, "Password123!");

        context.Users.AddRange(organiser1, organiser2, participant1, participant2);
        context.SaveChanges();

        var event1 = new Event
        {
            OrganiserId = organiser1.UserId,
            Name = "Pretoria Spring Marathon",
            EventDate = new DateOnly(2026, 10, 18),
            Location = "Union Buildings, Pretoria",
            Description = "Annual road race through the city centre.",
            Distance = 42.20m,
            Type = Models.EventType.Run,
            Status = EventStatus.Open
        };

        var event2 = new Event
        {
            OrganiserId = organiser1.UserId,
            Name = "Fun Run for Charity",
            EventDate = new DateOnly(2026, 11, 8),
            Location = "Pretoria Botanical Garden",
            Description = "Family-friendly fun run supporting local charities.",
            Distance = 5.00m,
            Type = Models.EventType.Run,
            Status = EventStatus.Planned
        };

        var event3 = new Event
        {
            OrganiserId = organiser2.UserId,
            Name = "Gauteng Trail Challenge",
            EventDate = new DateOnly(2026, 9, 27),
            Location = "Magaliesberg Trail Park",
            Description = "Off-road trail running event across three distances.",
            Distance = 21.00m,
            Type = Models.EventType.Run,
            Status = EventStatus.Open
        };

        context.Events.AddRange(event1, event2, event3);
        context.SaveChanges();

        var cat1 = new Category { EventId = event1.EventId, Name = "42km Marathon", DistanceKm = 42.20m, MinAge = 18, EntryFee = 350.00m, MaxParticipants = 500 };
        var cat2 = new Category { EventId = event1.EventId, Name = "21km Half Marathon", DistanceKm = 21.10m, MinAge = 16, EntryFee = 250.00m, MaxParticipants = 800 };
        var cat3 = new Category { EventId = event2.EventId, Name = "5km Fun Run", DistanceKm = 5.00m, EntryFee = 100.00m, MaxParticipants = 1000 };
        var cat4 = new Category { EventId = event3.EventId, Name = "10km Trail", DistanceKm = 10.00m, MinAge = 14, EntryFee = 150.00m, MaxParticipants = 300 };
        var cat5 = new Category { EventId = event3.EventId, Name = "21km Trail", DistanceKm = 21.00m, MinAge = 18, EntryFee = 220.00m, MaxParticipants = 200 };

        context.Categories.AddRange(cat1, cat2, cat3, cat4, cat5);
        context.SaveChanges();

        var enrolment1 = new Enrolment { ParticipantId = participant1.UserId, CategoryId = cat1.CategoryId, BibNumber = "A1001", Status = EnrolmentStatus.Registered };
        var enrolment2 = new Enrolment { ParticipantId = participant1.UserId, CategoryId = cat4.CategoryId, BibNumber = "A1002", Status = EnrolmentStatus.Registered };
        var enrolment3 = new Enrolment { ParticipantId = participant2.UserId, CategoryId = cat2.CategoryId, BibNumber = "A2001", Status = EnrolmentStatus.CheckedIn };
        var enrolment4 = new Enrolment { ParticipantId = participant2.UserId, CategoryId = cat3.CategoryId, BibNumber = "A2002", Status = EnrolmentStatus.Registered };

        context.Enrolments.AddRange(enrolment1, enrolment2, enrolment3, enrolment4);
        context.SaveChanges();

        context.Results.Add(new Result { EnrolmentId = enrolment3.EnrolmentId, FinishTime = new TimeOnly(1, 45, 32), Position = 12, Status = ResultStatus.Finished });

        context.Payments.AddRange(
            new Payment { EnrolmentId = enrolment1.EnrolmentId, Amount = 350.00m, PaymentMethod = "Card", Status = PaymentStatus.Completed },
            new Payment { EnrolmentId = enrolment2.EnrolmentId, Amount = 150.00m, PaymentMethod = "EFT", Status = PaymentStatus.Completed },
            new Payment { EnrolmentId = enrolment3.EnrolmentId, Amount = 250.00m, PaymentMethod = "Card", Status = PaymentStatus.Completed },
            new Payment { EnrolmentId = enrolment4.EnrolmentId, Amount = 100.00m, PaymentMethod = "Card", Status = PaymentStatus.Pending }
        );

        context.SaveChanges();
    }
}
