using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Selu383.SP26.Api.Data;
using Selu383.SP26.Api.Features.Locations;
using System.Security.Claims;

namespace Selu383.SP26.Api.Controllers;

[Route("api/locations")]
[ApiController]
public class LocationsController(DataContext dataContext) : ControllerBase
{
    [HttpGet]
    public IQueryable<LocationDto> GetAll()
    {
        return dataContext.Locations.Select(x => new LocationDto
        {
            Id = x.Id,
            Name = x.Name,
            Address = x.Address,
            TableCount = x.TableCount,
            ManagerId = x.ManagerId
        });
    }

    [HttpGet("{id}")]
    public ActionResult<LocationDto> GetById(int id)
    {
        var location = dataContext.Locations.FirstOrDefault(x => x.Id == id);
        if (location == null) return NotFound();

        return Ok(new LocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Address = location.Address,
            TableCount = location.TableCount,
            ManagerId = location.ManagerId
        });
    }

    // Admin only
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult<LocationDto> Create(LocationDto dto)
    {
        if (!IsValid(dto)) return BadRequest();

        var location = new Location
        {
            Name = dto.Name!.Trim(),
            Address = dto.Address!.Trim(),
            TableCount = dto.TableCount,
            ManagerId = dto.ManagerId
        };

        dataContext.Locations.Add(location);
        dataContext.SaveChanges();

        // Return what was saved (includes ManagerId)
        var result = new LocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Address = location.Address,
            TableCount = location.TableCount,
            ManagerId = location.ManagerId
        };

        return CreatedAtAction(nameof(GetById), new { id = location.Id }, result);
    }

    // Admin OR manager
    [Authorize]
    [HttpPut("{id}")]
    public ActionResult<LocationDto> Update(int id, LocationDto dto)
    {
        if (!IsValid(dto)) return BadRequest();

        var location = dataContext.Locations.FirstOrDefault(x => x.Id == id);
        if (location == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");

        // Claims are strings; parse to int to compare to ManagerId (int?)
        int? userId = null;
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdStr, out var parsed)) userId = parsed;

        if (!isAdmin && location.ManagerId != userId)
            return Forbid();

        location.Name = dto.Name!.Trim();
        location.Address = dto.Address!.Trim();
        location.TableCount = dto.TableCount;

        // Admin can change manager; manager cannot
        if (isAdmin)
            location.ManagerId = dto.ManagerId;

        dataContext.SaveChanges();

        return Ok(new LocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Address = location.Address,
            TableCount = location.TableCount,
            ManagerId = location.ManagerId
        });
    }

    // Admin OR manager
    [Authorize]
    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        var location = dataContext.Locations.FirstOrDefault(x => x.Id == id);
        if (location == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");

        int? userId = null;
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdStr, out var parsed)) userId = parsed;

        if (!isAdmin && location.ManagerId != userId)
            return Forbid();

        dataContext.Locations.Remove(location);
        dataContext.SaveChanges();

        return Ok();
    }

    private static bool IsValid(LocationDto dto)
    {
        if (dto == null) return false;
        if (string.IsNullOrWhiteSpace(dto.Name)) return false;
        if (dto.Name.Trim().Length > 120) return false;
        if (string.IsNullOrWhiteSpace(dto.Address)) return false;
        if (dto.TableCount < 1) return false;
        return true;
    }
}