using DietitianClinic.Business.Exceptions;
using DietitianClinic.Business.Services;
using DietitianClinic.DataAccess.Context;
using DietitianClinic.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using DataRepos = DietitianClinic.DataAccess.Repositories;

namespace DietitianClinic.Tests.Unit;

/// <summary>
/// InMemory DbContext kullanan PatientService testleri.
/// _context gerektiren metotlar (GetPatientByIdAsync, UpdatePatientAsync, DeletePatientAsync) burada test edilir.
/// </summary>
public class PatientServiceDbTests : IDisposable
{
    private readonly DietitianClinicDbContext _context;
    private readonly Mock<DataRepos.IUnitOfWork> _uow = new();
    private readonly Mock<DataRepos.IRepository<Patient>> _patientRepo = new();

    public PatientServiceDbTests()
    {
        var opts = new DbContextOptionsBuilder<DietitianClinicDbContext>()
            .UseInMemoryDatabase("PatientServiceDb_" + Guid.NewGuid())
            .Options;
        _context = new DietitianClinicDbContext(opts);

        _uow.Setup(u => u.PatientRepository)
            .Returns((DataRepos.IRepository<Patient>)_patientRepo.Object);
    }

    private PatientService CreateSut() =>
        new PatientService(_uow.Object, _context, NullLogger<PatientService>.Instance);

    // ── GetPatientByIdAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetPatientById_ThrowsNotFoundException_WhenPatientDoesNotExist()
    {
        // NotFoundException try içinde fırlatılır → catch (line 79-80) LogError + throw tetiklenir
        var sut = CreateSut();
        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetPatientByIdAsync(999));
    }

    [Fact]
    public async Task GetPatientById_FiltersByUserId_WhenDietitianOnly()
    {
        // dietitianOnly=true && userId.HasValue → line 67-68 dalı kapsamı
        var patient = new Patient
        {
            Id = 10, FirstName = "Diyet", LastName = "Hasta",
            Email = "d@test.com", Phone = "05559999999",
            Address = "Adres", City = "İzmir",
            MedicalHistory = "", Allergies = "", Notes = "",
            IsDeleted = false, UserId = 5
        };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var sut    = CreateSut();
        var result = await sut.GetPatientByIdAsync(10, userId: 5, dietitianOnly: true);

        Assert.Equal(10, result.Id);
    }

    // ── UpdatePatientAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePatient_ReturnsTrue_WhenPatientExists()
    {
        var patient = new Patient
        {
            Id = 1, FirstName = "Ali", LastName = "Veli",
            Email = "ali@test.com", Phone = "05551111111",
            Address = "Test Adres", City = "İstanbul",
            MedicalHistory = "", Allergies = "", Notes = "",
            IsDeleted = false
        };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        _patientRepo.Setup(r => r.UpdateAsync(It.IsAny<Patient>())).ReturnsAsync(1);

        var sut    = CreateSut();
        var result = await sut.UpdatePatientAsync(1, new Patient { FirstName = "Yeni", LastName = "İsim" });

        Assert.True(result);
    }

    [Fact]
    public async Task UpdatePatient_Throws_WhenPatientNotFound()
    {
        // GetPatientByIdAsync NotFoundException → UpdateAsync catch LogError tetiklenir
        var sut = CreateSut();
        await Assert.ThrowsAsync<NotFoundException>(() => sut.UpdatePatientAsync(999, new Patient()));
    }

    // ── DeletePatientAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DeletePatient_ReturnsTrue_WhenPatientExists()
    {
        var patient = new Patient
        {
            Id = 2, FirstName = "Test", LastName = "Hasta",
            Email = "test@test.com", Phone = "05551234567",
            Address = "Test Adres", City = "Ankara",
            MedicalHistory = "", Allergies = "", Notes = "",
            IsDeleted = false, Gender = Gender.Other,
            BirthDate = new DateTime(1990, 1, 1)
        };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        _patientRepo.Setup(r => r.DeleteAsync(It.IsAny<Patient>())).ReturnsAsync(1);

        var sut    = CreateSut();
        var result = await sut.DeletePatientAsync(2);

        Assert.True(result);
    }

    [Fact]
    public async Task DeletePatient_Throws_WhenPatientNotFound()
    {
        // catch bloğu LogError → re-throw (yeni satır kapsamı)
        var sut = CreateSut();
        await Assert.ThrowsAsync<NotFoundException>(() => sut.DeletePatientAsync(999));
    }

    public void Dispose() => _context.Dispose();
}
