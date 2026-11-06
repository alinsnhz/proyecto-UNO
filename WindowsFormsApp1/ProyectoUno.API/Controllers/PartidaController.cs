using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ProyectoUno.API;
using ProyectoUno.API.DTOs;
using System.Linq;
using System.Data;

namespace ProyectoUno.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartidaController : ControllerBase
    {
        private readonly HistorialDAO _historialDAO = new HistorialDAO();
        private readonly LogJuegoDAO _logJuegoDAO = new LogJuegoDAO();
        private readonly JugadorDAO _jugadorDAO = new JugadorDAO();

        private List<Dictionary<string, object>> ConvertirTabla(DataTable tabla)
        {
            var resultado = new List<Dictionary<string, object>>();

            foreach (DataRow fila in tabla.Rows)
            {
                var registro = new Dictionary<string, object>();

                foreach (DataColumn columna in tabla.Columns)
                {
                    registro[columna.ColumnName] =
                        fila[columna] == DBNull.Value ? null : fila[columna];
                }

                resultado.Add(registro);
            }

            return resultado;
        }

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

        [HttpPost("finalizar")]
        public IActionResult FinalizarPartida([FromBody] FinalizarPartidaDto dto)
        {
            if (dto == null ||
                dto.IdPartida <= 0 ||
                dto.IdJugadorGanador <= 0 ||
                dto.Resultados == null ||
                dto.Resultados.Count == 0)
            {
                return BadRequest("Datos de finalización inválidos.");
            }

            var resultados = dto.Resultados.ToDictionary(
                x => x.IdJugador,
                x => x.CartasRestantes
            );

            bool finalizada = _historialDAO.FinalizarPartida(
                dto.IdPartida,
                dto.IdJugadorGanador,
                resultados
            );

            if (!finalizada)
                return Conflict("La partida ya fue finalizada o no se pudo finalizar.");

            return Ok(new { Mensaje = "Partida finalizada correctamente." });
        }

        // Consulta el historial general de partidas.
        [HttpGet("historial")]
        public IActionResult ObtenerHistorial()
        {
            var historial = _historialDAO.ConsultarHistorial();
            return Ok(ConvertirTabla(historial));
        }

        // Consulta los eventos de una partida específica.
        [HttpGet("historial/{idPartida:int}")]
        public IActionResult ObtenerLogDePartida(int idPartida)
        {
            if (idPartida <= 0)
                return BadRequest("El ID de partida no es válido.");

            var log = _logJuegoDAO.ObtenerLogDePartida(idPartida);
            return Ok(ConvertirTabla(log));
        }
    }
}