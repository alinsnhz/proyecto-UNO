using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using proyectoUNO;
using ProyectoUno.API.DTOs;
using WindowsFormsApp1;

namespace ProyectoUno.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartidaController : ControllerBase
    {
        private readonly HistorialDAO _historialDAO = new HistorialDAO();
        private readonly LogJuegoDAO _logJuegoDAO = new LogJuegoDAO();
        private readonly JugadorDAO _jugadorDAO = new JugadorDAO();

        [HttpPost("iniciar")]
        public IActionResult IniciarPartida([FromBody] IniciarPartidaDto dto)
        {
            List<int> ids = new List<int>();

            foreach (string nombre in dto.NombresJugadores)
            {
                int id = _jugadorDAO.ObtenerOCrear(nombre);
                ids.Add(id);
            }

            int idPartida = _historialDAO.GuardarPartida();
            _historialDAO.GuardarParticipantes(idPartida, dto.NombresJugadores);

            return Ok(new
            {
                IdPartida = idPartida,
                IdsJugadores = ids
            });
        }

        [HttpPost("log-jugada")]
        public IActionResult RegistrarJugada([FromBody] RegistrarJugadaDto dto)
        {
            Carta carta = new Carta(dto.Carta.Color, dto.Carta.Valor, dto.Carta.Tipo);
            _logJuegoDAO.RegistrarCartaJugada(dto.IdPartida, dto.IdJugador, dto.NombreJugador, carta?.ToString() ?? "");
            return Ok(new { Mensaje = "Jugada registrada correctamente" });
        }

        [HttpPost("log-turno")]
        public IActionResult RegistrarTurno([FromBody] RegistrarJugadaDto dto)
        {
            _logJuegoDAO.RegistrarTurno(dto.IdPartida, dto.IdJugador, dto.NombreJugador);
            return Ok(new { Mensaje = "Turno registrado correctamente" });
        }
    }
}