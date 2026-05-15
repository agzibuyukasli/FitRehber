using System.Linq.Expressions;
using DietitianClinic.Business.Exceptions;
using DietitianClinic.Business.Services;
using DietitianClinic.Entity.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using DataRepos = DietitianClinic.DataAccess.Repositories;

namespace DietitianClinic.Tests.Unit;

public class PatientServiceTests
{
    private readonly Mock<DataRepos.IUnitOfWork>           _uow         = new();
    private readonly Mock<DataRepos.IRepository<Patient>>  _patientRepo = new();

    private PatientService CreateSut()
    {
        _uow.Setup(u => u.PatientRepository)
            .Returns((DataRepos.IRepository<Patient>)_patientRepo.Object);

        // DbContext, test edilen metotlarda (_CreatePatient, _GetPatientsPaginated) kullanılmıyor.
        // null! ile güvenle geçilebilir.
        return new PatientService(
            _uow.Object,
            null!,
            NullLogger<PatientService>.Instance);
    }

    // ─── CreatePatientAsync ───────────────────────────────────────────

    [Fact]
    public async Task CreatePatient_ThrowsValidation_WhenEmailAlreadyExists()
    {
        var existing = new Patient { Email = "dup@test.com" };
        _patientRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Patient, bool>>>()))
            .ReturnsAsync(existing);

        var sut = CreateSut();

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.CreatePatientAsync(new Patient { Email = "dup@test.com" }));
    }

    [Fact]
    public async Task CreatePatient_SetsIsActiveTrue_AndReturnsId()
    {
        _patientRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Patient, bool>>>()))
            .ReturnsAsync((Patient?)null);
        _patientRepo
            .Setup(r => r.AddAsync(It.IsAny<Patient>()))
            .Callback<Patient>(p => p.Id = 99)
            .ReturnsAsync(1);

        var sut    = CreateSut();
        var result = await sut.CreatePatientAsync(new Patient { Email = "new@test.com" });

        Assert.Equal(99, result);
    }

    [Fact]
    public async Task CreatePatient_SkipsEmailCheck_WhenEmailIsEmpty()
    {
        _patientRepo
            .Setup(r => r.AddAsync(It.IsAny<Patient>()))
            .ReturnsAsync(1);

        var sut = CreateSut();
        await sut.CreatePatientAsync(new Patient { Email = "" });

        _patientRepo.Verify(
            r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Patient, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task CreatePatient_SetsUserId_WhenProvided()
    {
        _patientRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Patient, bool>>>()))
            .ReturnsAsync((Patient?)null);
        _patientRepo.Setup(r => r.AddAsync(It.IsAny<Patient>())).ReturnsAsync(1);

        var patient = new Patient { Email = "u@test.com" };
        var sut     = CreateSut();
        await sut.CreatePatientAsync(patient, userId: 7);

        Assert.Equal(7, patient.UserId);
    }

    [Fact]
    public async Task CreatePatient_DoesNotSetUserId_WhenNotProvided()
    {
        _patientRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Patient, bool>>>()))
            .ReturnsAsync((Patient?)null);
        _patientRepo.Setup(r => r.AddAsync(It.IsAny<Patient>())).ReturnsAsync(1);

        var patient = new Patient { Email = "u@test.com" };
        var sut     = CreateSut();
        await sut.CreatePatientAsync(patient, userId: null);

        Assert.Null(patient.UserId);
    }

    // ─── GetPatientsPaginatedAsync ────────────────────────────────────

    [Fact]
    public async Task GetPatientsPaginated_ReturnsPatientsAndTotalCount()
    {
        var page = new List<Patient>
        {
            new() { FirstName = "Ali" },
            new() { FirstName = "Veli" }
        };
        _patientRepo.Setup(r => r.GetPagedAsync(1, 10)).ReturnsAsync(page);
        _patientRepo.Setup(r => r.CountAsync()).ReturnsAsync(42);

        var sut             = CreateSut();
        var (patients, total) = await sut.GetPatientsPaginatedAsync(1, 10);

        Assert.Equal(2,  patients.Count);
        Assert.Equal(42, total);
    }
}
