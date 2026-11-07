using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ProyectoUno.API;
using ProyectoUno.API.DTOs;

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
            if (dto == null || dto.NombresJugadores == null)
            {
                return BadRequest(new
                {
                    Mensaje = "La lista de jugadores es obligatoria."
                });
            }

            if (dto.NombresJugadores.Count != 3)
            {
                return BadRequest(new
                {
                    Mensaje = "La partida debe tener exactamente 3 jugadores."
                });
            }

            if (dto.NombresJugadores.Exists(string.IsNullOrWhiteSpace))
            {
                return BadRequest(new
                {
                    Mensaje = "Todos los jugadores deben tener un nombre."
                });
            }

            try
            {
                ResultadoInicioPartida resultado =
                    _historialDAO.GuardarPartidaConParticipantes(dto.NombresJugadores);

                return Ok(new
                {
                    IdPartida = resultado.IdPartida,
                    IdsJugadores = resultado.IdsJugadores
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al iniciar partida: " + ex.Message);

                return StatusCode(500, new
                {
                    Mensaje = "No se pudo registrar la partida en la base de datos."
                });
            }
        }

        [HttpPost("log-jugada")]
        public IActionResult RegistrarJugada([FromBody] RegistrarJugadaDto dto)
        {
            if (dto.Carta != null)
            {
                Carta carta = new Carta(dto.Carta.Color ?? "", dto.Carta.Valor ?? "", dto.Carta.Tipo ?? "");
                _logJuegoDAO.RegistrarCartaJugada(dto.IdPartida, dto.IdJugador, dto.NombreJugador ?? "", carta.ToString());
            }
            return Ok(new { Mensaje = "Jugada registrada correctamente" });
        }

        [HttpPost("log-turno")]
        public IActionResult RegistrarTurno([FromBody] RegistrarJugadaDto dto)
        {
            _logJuegoDAO.RegistrarTurno(dto.IdPartida, dto.IdJugador, dto.NombreJugador ?? "");
            return Ok(new { Mensaje = "Turno registrado correctamente" });
        }

        [HttpPost("log-robar")]
        public IActionResult RegistrarCartaRobada([FromBody] RegistrarJugadaDto dto)
        {
            if (dto.Carta != null)
            {
                Carta carta = new Carta(
                    dto.Carta.Color ?? "",
                    dto.Carta.Valor ?? "",
                    dto.Carta.Tipo ?? ""
                );

                _logJuegoDAO.RegistrarCartaRobada(
                    dto.IdPartida,
                    dto.IdJugador,
                    dto.NombreJugador ?? "",
                    carta.ToString()
                );
            }

            return Ok(new { Mensaje = "Carta robada registrada correctamente" });
        }
    }
}