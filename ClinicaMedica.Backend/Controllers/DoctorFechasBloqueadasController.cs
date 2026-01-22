using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaMedica.Backend.Controllers
{
   
    
        [ApiController]
        [Route("api/[controller]")]
        public class DoctorFechasBloqueadasController : ControllerBase
        {
            private readonly AppDbContext _context;
            public DoctorFechasBloqueadasController(AppDbContext context) => _context = context;

            [HttpGet("{doctorId:int}")]
            public async Task<IActionResult> Get(int doctorId)
            {
                var items = await _context.DoctorFechasBloqueadas
                    .AsNoTracking()
                    .Where(x => x.DoctorID == doctorId)
                    .OrderBy(x => x.Fecha)
                    .ToListAsync();

                return Ok(items);
            }

            [HttpPost]
            [Authorize(Roles = "Doctor,Admin")]
            public async Task<IActionResult> Bloquear([FromBody] DoctorFechasBloqueadas item)
            {
                item.Fecha = item.Fecha.Date;
                _context.DoctorFechasBloqueadas.Add(item);
                await _context.SaveChangesAsync();
                return Ok(item);
            }

            [HttpDelete("{id:int}")]
            [Authorize(Roles = "Doctor,Admin")]
            public async Task<IActionResult> Delete(int id)
            {
                var item = await _context.DoctorFechasBloqueadas.FindAsync(id);
                if (item == null) return NotFound();
                _context.DoctorFechasBloqueadas.Remove(item);
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }
    }

