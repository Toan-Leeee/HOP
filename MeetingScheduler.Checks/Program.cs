using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Models;
using MeetingScheduler.Web.Models.Enums;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Common;
using MeetingScheduler.Web.ViewModels.Account;
using MeetingScheduler.Web.ViewModels.Departments;
using MeetingScheduler.Web.ViewModels.Meetings;
using MeetingScheduler.Web.ViewModels.Reports;
using MeetingScheduler.Web.ViewModels.Rooms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MeetingScheduler.Web.Services.Email;

// Always replace the catalog; never migrate or delete the supplied database.
var database = "MeetingSchedulerChecks_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfffffff");
var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MEETING_CHECKS_CONNECTION")
    ?? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true")
{ InitialCatalog = "master", ConnectTimeout = 15 };
bool created = false;
int passed = 0, failed = 0;
Console.WriteLine($"SQL Server: {connection.DataSource}; isolated database: {database}");
try
{
    await using (var master = new SqlConnection(connection.ConnectionString))
    {
        await master.OpenAsync();
        await using var command = master.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{database}]";
        await command.ExecuteNonQueryAsync();
        created = true;
    }
    connection.InitialCatalog = database;
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddMemoryCache();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddSingleton<IEmailQueue, EmailQueue>();
    services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connection.ConnectionString));
    // Resolve the actual backend implementation for each published service contract.
    foreach (var contract in typeof(IRoomService).Assembly.GetTypes().Where(t => t.IsInterface && t.Namespace == typeof(IRoomService).Namespace))
    {
        var implementation = contract.Assembly.GetTypes().SingleOrDefault(t => t.IsClass && !t.IsAbstract && contract.IsAssignableFrom(t));
        if (implementation != null) services.AddScoped(contract, implementation);
    }
    await using var provider = services.BuildServiceProvider();
    await using (var scope = provider.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await SeedData.InitializeAsync(db);
    }
    int admin, organizer, employee, outsider;
    await using (var scope = provider.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        admin = await db.Users.Where(u => u.Email == "admin@company.test").Select(u => u.UserId).SingleAsync();
        organizer = await db.Users.Where(u => u.Email == "organizer@company.test").Select(u => u.UserId).SingleAsync();
        employee = await db.Users.Where(u => u.Email == "employee@company.test").Select(u => u.UserId).SingleAsync();
        outsider = await db.Users.Where(u => u.Email == "hieu@company.test").Select(u => u.UserId).SingleAsync();
    }
    async Task Check(string name, Func<IServiceProvider, Task> body)
    {
        await using var scope = provider.CreateAsyncScope();
        try { await body(scope.ServiceProvider); passed++; Console.WriteLine($"PASS {name}"); }
        catch (Exception e) { failed++; Console.WriteLine($"FAIL {name}: {e.GetType().Name}: {e.Message}"); }
    }
    int room = 0, meeting = 0;
    var day = DateTime.Today.AddDays(60).AddHours(9);
    MeetingEditViewModel NewMeeting(string title, DateTime start, int roomId) => new()
    { Title = title, RoomId = roomId, StartTime = start, EndTime = start.AddHours(1), ParticipantIds = [employee] };
    await Check("Room CRUD, search, sort, paging, cache invalidation", async sp =>
    {
        var s = sp.GetRequiredService<IRoomService>();
        await s.GetLookupAsync();
        foreach (var capacity in new[] { 2, 5, 8 })
        {
            var result = await s.SaveAsync(new() { RoomName = $"Checks Room {capacity}", Capacity = capacity, Status = RoomStatus.Active }, admin);
            Success(result); if (capacity == 2) room = result.EntityId!.Value;
        }
        Require((await s.GetLookupAsync()).Any(x => x.Id == room), "Create must invalidate lookup cache");
        var page = await s.QueryAsync(new() { SearchString = "Checks Room", SortOrder = "capacity_desc", PageSize = 1, Page = 2 });
        Require(page.TotalCount == 3 && page.Items.Single().Capacity == 5 && page.TotalPages == 3, "Filter/count/sort/page mismatch");
        var normalized = await s.QueryAsync(new() { Page = -1, PageSize = 0 });
        Require(normalized.Page == 1 && normalized.PageSize == 10, "Invalid paging not normalized");
        var model = (await s.GetAsync(room))!; model.Location = "Checks location";
        Success(await s.SaveAsync(model, admin));
        Require((await s.GetAsync(room))!.Location == model.Location, "Edit not persisted");
        Denied(await s.SaveAsync(new() { RoomName = model.RoomName, Capacity = 2 }, admin));
        Denied(await s.SaveAsync(new() { RoomName = "Invalid", Capacity = 0 }, admin));
    });
    await Check("Role permissions for administration and meeting ownership", async sp =>
    {
        Denied(await sp.GetRequiredService<IRoomService>().SaveAsync(new() { RoomName = "Forbidden", Capacity = 3 }, employee));
        Denied(await sp.GetRequiredService<IDepartmentService>().SaveAsync(new() { DepartmentName = "Forbidden" }, organizer));
        Denied(await sp.GetRequiredService<IUserService>().DisableAsync(admin, employee));
        Denied(await sp.GetRequiredService<IMeetingService>().SaveAsync(NewMeeting("Forbidden", day, room), employee));
    });
    await Check("Account passwords, lock/unlock and session validation", async sp =>
    {
        var accounts = sp.GetRequiredService<IAccountService>();
        var users = sp.GetRequiredService<IUserService>();
        LoginViewModel Login(string password) => new() { Email = "employee@company.test", Password = password };
        Require((await accounts.LoginAsync(Login("Demo@12345")))?.UserId == employee, "Valid password rejected");
        Require(await accounts.LoginAsync(Login("Wrong@12345")) == null, "Invalid password accepted");
        Require(await accounts.ValidateSessionAsync(employee, "Employee"), "Active session rejected");
        Require(!await accounts.ValidateSessionAsync(employee, "Admin"), "Incorrect role claim accepted");
        Success(await users.DisableAsync(employee, admin));
        Require(await accounts.LoginAsync(Login("Demo@12345")) == null, "Locked account can log in");
        Require(!await accounts.ValidateSessionAsync(employee, "Employee"), "Locked account session accepted");
        var model = (await users.GetAsync(employee))!; model.IsDisabled = false;
        Success(await users.UpdateAsync(model, admin));
        Require(await accounts.ValidateSessionAsync(employee, "Employee"), "Unlocked account session eligibility not restored");
        Require(await accounts.LoginAsync(Login("Demo@12345")) != null, "Unlocked account cannot log in");
        Denied(await accounts.ChangePasswordAsync(employee, new()
        { CurrentPassword = "Wrong@12345", NewPassword = "Checks@New12345", ConfirmPassword = "Checks@New12345" }));
        Denied(await accounts.ChangePasswordAsync(employee, new()
        { CurrentPassword = "Demo@12345", NewPassword = "Checks@New12345", ConfirmPassword = "Mismatch@12345" }));
        Success(await accounts.ChangePasswordAsync(employee, new()
        { CurrentPassword = "Demo@12345", NewPassword = "Checks@New12345", ConfirmPassword = "Checks@New12345" }));
        Require(await accounts.LoginAsync(Login("Demo@12345")) == null, "Old password still accepted");
        Require((await accounts.LoginAsync(Login("Checks@New12345")))?.UserId == employee, "New password rejected");
    });
    await Check("Meeting creation, overlap rejection, adjacency, capacity, availability", async sp =>
    {
        var s = sp.GetRequiredService<IMeetingService>();
        var result = await s.SaveAsync(NewMeeting("Checks baseline", day, room), organizer); Success(result); meeting = result.EntityId!.Value;
        Denied(await s.SaveAsync(NewMeeting("Checks overlap", day.AddMinutes(30), room), organizer));
        Success(await s.SaveAsync(NewMeeting("Checks adjacent", day.AddHours(1), room), organizer));
        var tooMany = NewMeeting("Checks capacity", day.AddDays(1), room); tooMany.ParticipantIds = [employee, outsider, admin];
        Denied(await s.SaveAsync(tooMany, organizer));
        var availability = sp.GetRequiredService<IRoomAvailabilityService>();
        Require(!(await availability.GetAvailableAsync(day, day.AddHours(1))).Any(r => r.RoomId == room), "Occupied room returned");
        Require((await availability.GetAvailableAsync(day.AddHours(2), day.AddHours(3))).Any(r => r.RoomId == room), "Adjacent free room missing");
        Require(!(await availability.GetAvailableAsync(day.AddHours(2), day.AddHours(3), 3)).Any(r => r.RoomId == room), "Capacity filter ignored");
    });
    await Check("Stale RowVersion across independent contexts and ownership", async sp =>
    {
        var s = sp.GetRequiredService<IMeetingService>();
        var original = (await s.GetAsync(meeting, organizer))!;
        Require(original.RowVersion.Length == 8, "SQL Server rowversion missing");
        await using var other = provider.CreateAsyncScope();
        var second = other.ServiceProvider.GetRequiredService<IMeetingService>();
        var stale = (await second.GetAsync(meeting, organizer))!;
        original.Title = "Checks updated"; Success(await s.SaveAsync(original, organizer));
        stale.Title = "Checks stale"; Denied(await second.SaveAsync(stale, organizer));
        var current = (await s.GetAsync(meeting, organizer))!;
        Require(current.Title == original.Title, "Stale write replaced winner");
        Denied(await s.SaveAsync(current, employee));
        Denied(await s.CancelAsync(new() { MeetingId = meeting, CancelReason = "Unauthorized", RowVersion = current.RowVersion }, employee));
        Denied(await s.CancelAsync(new() { MeetingId = meeting, CancelReason = "Stale cancellation", RowVersion = stale.RowVersion }, admin));
    });
    await Check("Concurrent overlapping reservations have exactly one winner", async sp =>
    {
        using var barrier = new Barrier(2);
        async Task<ServiceResult> Reserve(string title)
        {
            await using var scope = provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IMeetingService>();
            Require(barrier.SignalAndWait(TimeSpan.FromSeconds(20)), "Reservation barrier timeout");
            return await service.SaveAsync(NewMeeting(title, day.AddDays(3), room), organizer);
        }
        var results = await Task.WhenAll(Task.Run(() => Reserve("Checks concurrent A")), Task.Run(() => Reserve("Checks concurrent B")));
        Require(results.Count(r => r.Succeeded) == 1, "Concurrent reservations must have exactly one successful result");
        var db = sp.GetRequiredService<ApplicationDbContext>();
        Require(await db.Meetings.CountAsync(m => m.RoomId == room && m.StartTime == day.AddDays(3) && m.Status == MeetingStatus.Confirmed) == 1, "Duplicate reservation persisted");
    });
    await Check("RSVP Pending/Accepted/Declined, timestamps, filters and nonparticipant", async sp =>
    {
        var s = sp.GetRequiredService<IRsvpService>();
        var pending = await s.QueryAsync(new() { MeetingId = meeting, Status = RsvpStatus.Pending }, organizer);
        Require(pending?.Items.Single().RespondedAt == null, "Initial Pending must have no response timestamp");
        Success(await s.RespondAsync(meeting, employee, RsvpStatus.Accepted));
        var accepted = await s.QueryAsync(new() { MeetingId = meeting, Status = RsvpStatus.Accepted, SearchString = "employee@company.test", PageSize = 1 }, organizer);
        Require(accepted?.TotalCount == 1 && accepted.Items.Single().RespondedAt != null, "Accepted response missing");
        Success(await s.RespondAsync(meeting, employee, RsvpStatus.Declined));
        Require((await s.QueryAsync(new() { MeetingId = meeting, Status = RsvpStatus.Declined }, organizer))?.TotalCount == 1, "Declined response missing");
        Denied(await s.RespondAsync(meeting, outsider, RsvpStatus.Accepted));
        Denied(await s.RespondAsync(meeting, employee, (RsvpStatus)99));
    });
    await Check("Reports agree with persisted SQL data and room/date filters", async sp =>
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var report = await sp.GetRequiredService<IReportService>().GetAsync(new() { From = day.Date, To = day.Date.AddDays(1), RoomId = room });
        var rows = await db.Meetings.AsNoTracking().Where(m => m.RoomId == room && m.StartTime >= day.Date && m.StartTime < day.Date.AddDays(1)).ToListAsync();
        Require(report.TotalMeetings == rows.Count && report.CancelledMeetings == rows.Count(m => m.Status == MeetingStatus.Cancelled), "Report counts mismatch");
        Require(report.Rooms.Single().MeetingCount == rows.Count(m => m.Status == MeetingStatus.Confirmed), "Room count mismatch");
        Require(Math.Abs(report.Rooms.Single().TotalHours - rows.Where(m => m.Status == MeetingStatus.Confirmed).Sum(m => (m.EndTime - m.StartTime).TotalHours)) < .001, "Room duration mismatch");
        Require(report.DeclinedResponses == 1 && report.PendingResponses == 1 && report.AcceptedResponses == 0, "RSVP report totals mismatch");
    });
    await Check("Cancellation, soft disable room/user/department retains history and audit", async sp =>
    {
        var s = sp.GetRequiredService<IMeetingService>();
        var current = (await s.GetAsync(meeting, admin))!;
        Success(await s.CancelAsync(new() { MeetingId = meeting, CancelReason = "Checks cancellation", RowVersion = current.RowVersion }, admin));
        Denied(await sp.GetRequiredService<IRsvpService>().RespondAsync(meeting, employee, RsvpStatus.Accepted));
        var rooms = sp.GetRequiredService<IRoomService>(); await rooms.GetLookupAsync();
        Success(await rooms.DisableAsync(room, admin));
        Require(!(await rooms.GetLookupAsync()).Any(r => r.Id == room), "Disabled room still cached");
        Denied(await s.SaveAsync(NewMeeting("Disabled room", day.AddDays(2), room), organizer));
        Success(await sp.GetRequiredService<IUserService>().DisableAsync(employee, admin));
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var department = await db.Users.Where(u => u.UserId == outsider).Select(u => u.DepartmentId).SingleAsync();
        Denied(await sp.GetRequiredService<IDepartmentService>().DisableAsync(department, admin));
        Success(await sp.GetRequiredService<IUserService>().DisableAsync(outsider, admin));
        Success(await sp.GetRequiredService<IDepartmentService>().DisableAsync(department, admin));
        db.ChangeTracker.Clear();
        var history = await db.Meetings.AsNoTracking().SingleAsync(m => m.MeetingId == meeting);
        Require(history.Status == MeetingStatus.Cancelled && history.CancelledByUserId == admin && history.CancelledAt != null, "Cancellation metadata missing");
        Require(await db.Rooms.AnyAsync(r => r.RoomId == room && r.IsDisabled), "Room history deleted");
        Require(await db.Users.AnyAsync(u => u.UserId == employee && u.IsDisabled), "User history deleted");
        Require(await db.Departments.AnyAsync(d => d.DepartmentId == department && d.IsDisabled), "Department history deleted");
        Require(await db.MeetingParticipants.AnyAsync(p => p.MeetingId == meeting && p.UserId == employee), "Participant history deleted");
        Require(await db.AuditLogs.AnyAsync(a => a.EntityName == "Room" && a.EntityId == room), "Room audit missing");
    });
}
catch (Exception e) { failed++; Console.WriteLine($"SETUP FAILURE: {e}"); }
finally
{
    if (created)
    {
        try
        {
            SqlConnection.ClearAllPools();
            connection.InitialCatalog = "master";
            await using var master = new SqlConnection(connection.ConnectionString);
            await master.OpenAsync();
            await using var command = master.CreateCommand();
            command.CommandText = $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}];";
            await command.ExecuteNonQueryAsync();
            command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = @name";
            command.Parameters.AddWithValue("@name", database);
            Require(Convert.ToInt32(await command.ExecuteScalarAsync()) == 0, "Database still exists");
            Console.WriteLine($"CLEANUP VERIFIED: {database} dropped");
        }
        catch (Exception e) { failed++; Console.WriteLine($"CLEANUP FAILURE ({database}): {e.Message}"); }
    }
}
Console.WriteLine($"RESULT: {passed} passed, {failed} failed");
return failed == 0 ? 0 : 1;

static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
static void Success(ServiceResult r) => Require(r.Succeeded, r.Error ?? "Expected success");
static void Denied(ServiceResult r) => Require(!r.Succeeded, "Expected rejected operation");
