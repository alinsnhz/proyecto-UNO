using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
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
            if (dto == null ||
                dto.NombresJugadores == null ||
                dto.NombresJugadores.Count != 3)
            {
                return BadRequest(new
                {
                    Mensaje = "La partida debe tener exactamente 3 jugadores."
                });
            }

            List<string> nombres = dto.NombresJugadores
                .Select(n => n?.Trim())
                .ToList();

            if (nombres.Any(string.IsNullOrWhiteSpace))
            {
                return BadRequest(new
                {
                    Mensaje = "Los nombres de los jugadores no pueden estar vacíos."
                });
            }

            if (nombres.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
            {
                return BadRequest(new
                {
                    Mensaje = "No se permiten jugadores repetidos."
                });
            }

            List<int> ids = new List<int>();

            foreach (string nombre in nombres)
            {
                int id = _jugadorDAO.ObtenerOCrear(nombre);
                ids.Add(id);
            }

            int idPartida = _historialDAO.GuardarPartida();
            _historialDAO.GuardarParticipantes(idPartida, nombres);

            return Ok(new
            {
                IdPartida = idPartida,
                IdsJugadores = ids,
                Mensaje = "Partida iniciada correctamente."
            });
        }

        [HttpPost("log-jugada")]
        public IActionResult RegistrarJugada([FromBody] RegistrarJugadaDto dto)
        {
            if (!JugadorValido(dto.IdJugador, dto.NombreJugador))
            {
                return BadRequest(new
                {
                    Mensaje = "El jugador indicado no existe o sus datos no coinciden."
                });
            }

            if (!JugadorPertenecePartida(dto.IdPartida, dto.IdJugador))
            {
                return BadRequest(new
                {
                    Mensaje = "El jugador no pertenece a esta partida."
                });
            }

            if (dto.Carta == null ||
                string.IsNullOrWhiteSpace(dto.Carta.Color) ||
                string.IsNullOrWhiteSpace(dto.Carta.Valor) ||
                string.IsNullOrWhiteSpace(dto.Carta.Tipo))
            {
                return BadRequest(new
                {
                    Mensaje = "Los datos de la carta están incompletos."
                });
            }

            Carta carta = new Carta(
                dto.Carta.Color.Trim(),
                dto.Carta.Valor.Trim(),
                dto.Carta.Tipo.Trim()
            );

            _logJuegoDAO.RegistrarCartaJugada(
                dto.IdPartida,
                dto.IdJugador,
                dto.NombreJugador.Trim(),
                carta.ToString()
            );

            return Ok(new
            {
                Mensaje = "Jugada registrada correctamente."
            });
        }

        [HttpPost("log-turno")]
        public IActionResult RegistrarTurno([FromBody] RegistrarTurnoDto dto)
        {
            if (!JugadorValido(dto.IdJugador, dto.NombreJugador))
            {
                return BadRequest(new
                {
                    Mensaje = "El jugador indicado no existe o sus datos no coinciden."
                });
            }

            if (!JugadorPertenecePartida(dto.IdPartida, dto.IdJugador))
            {
                return BadRequest(new
                {
                    Mensaje = "El jugador no pertenece a esta partida."
                });
            }

            _logJuegoDAO.RegistrarTurno(
                dto.IdPartida,
                dto.IdJugador,
                dto.NombreJugador.Trim()
            );

            return Ok(new
            {
                Mensaje = "Turno registrado correctamente."
            });
        }

        [HttpPost("log-robar")]
        public IActionResult RegistrarCartaRobada([FromBody] RegistrarJugadaDto dto)
        {
            if (!JugadorValido(dto.IdJugador, dto.NombreJugador))
            {
                return BadRequest(new
                {
                    Mensaje = "El jugador indicado no existe o sus datos no coinciden."
                });
            }

            if (!JugadorPertenecePartida(dto.IdPartida, dto.IdJugador))
            {
                return BadRequest(new
                {
                    Mensaje = "El jugador no pertenece a esta partida."
                });
            }

            if (dto.Carta == null ||
                string.IsNullOrWhiteSpace(dto.Carta.Color) ||
                string.IsNullOrWhiteSpace(dto.Carta.Valor) ||
                string.IsNullOrWhiteSpace(dto.Carta.Tipo))
            {
                return BadRequest(new
                {
                    Mensaje = "Los datos de la carta robada están incompletos."
                });
            }

            Carta carta = new Carta(
                dto.Carta.Color.Trim(),
                dto.Carta.Valor.Trim(),
                dto.Carta.Tipo.Trim()
            );

            _logJuegoDAO.RegistrarCartaRobada(
                dto.IdPartida,
                dto.IdJugador,
                dto.NombreJugador.Trim(),
                carta.ToString()
            );

            return Ok(new
            {
                Mensaje = "Carta robada registrada correctamente."
            });
        }

        [HttpPost("log-accion-especial")]
        public IActionResult RegistrarAccionEspecial(
            [FromBody] RegistrarAccionEspecialDto dto)
        {
            if (!JugadorValido(dto.IdJugador, dto.NombreJugador))
            {
                return BadRequest(new
                {
                    Mensaje = "El jugador indicado no existe o sus datos no coinciden."
                });
            }

            if (!JugadorPertenecePartida(dto.IdPartida, dto.IdJugador))
            {
                return BadRequest(new
                {
                    Mensaje = "El jugador no pertenece a esta partida."
                });
            }

            string[] accionesPermitidas =
            {
                "Salta", "Salto", "Reversa", "+2", "+4"
            };

            if (!accionesPermitidas.Contains(dto.TipoAccion))
            {
                return BadRequest(new
                {
                    Mensaje = "El tipo de acción especial no es válido."
                });
            }

            _logJuegoDAO.RegistrarAccionEspecial(
                dto.IdPartida,
                dto.IdJugador,
                dto.NombreJugador.Trim(),
                dto.TipoAccion
            );

            return Ok(new
            {
                Mensaje = "Acción especial registrada correctamente."
            });
        }

        [HttpPost("log-cambio-color")]
        public IActionResult RegistrarCambioColor(
            [FromBody] RegistrarCambioColorDto dto)
        {
            if (!JugadorValido(dto.IdJugador, dto.NombreJugador))
            {
                return BadRequest(new
                {
                    Mensaje = "El jugador indicado no existe o sus datos no coinciden."
                });
            }

            if (!JugadorPertenecePartida(dto.IdPartida, dto.IdJugador))
            {
                return BadRequest(new
                {
                    Mensaje = "El jugador no pertenece a esta partida."
                });
            }

            string[] coloresPermitidos =
            {
                "Rojo", "Azul", "Amarillo", "Verde"
            };

            if (!coloresPermitidos.Contains(
                dto.NuevoColor?.Trim(),
                StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    Mensaje = "El color seleccionado no es válido."
                });
            }

            _logJuegoDAO.RegistrarCambioColor(
                dto.IdPartida,
                dto.IdJugador,
                dto.NombreJugador.Trim(),
                dto.NuevoColor.Trim()
            );

            return Ok(new
            {
                Mensaje = "Cambio de color registrado correctamente."
            });
        }


        [HttpPost("finalizar")]
        public IActionResult FinalizarPartida(
            [FromBody] FinalizarPartidaDto dto)
        {
            if (dto == null ||
                dto.IdPartida <= 0 ||
                dto.IdJugadorGanador <= 0 ||
                dto.Resultados == null ||
                dto.Resultados.Count != 3)
            {
                return BadRequest(new
                {
                    Mensaje = "Los datos de finalización son inválidos. Se requieren los 3 jugadores."
                });
            }

            if (dto.Resultados.Any(r =>
                r == null ||
                r.IdJugador <= 0 ||
                r.CartasRestantes < 0))
            {
                return BadRequest(new
                {
                    Mensaje = "Los resultados de los jugadores son inválidos."
                });
            }

            // Evita jugadores repetidos.
            if (dto.Resultados
                .Select(r => r.IdJugador)
                .Distinct()
                .Count() != 3)
            {
                return BadRequest(new
                {
                    Mensaje = "No se permiten jugadores repetidos en los resultados."
                });
            }

            // Comprueba que los tres jugadores existan y pertenezcan a la partida.
            foreach (var resultado in dto.Resultados)
            {
                var jugador = _jugadorDAO.ObtenerPorId(resultado.IdJugador);

                if (jugador == null ||
                    !_historialDAO.JugadorPertenecePartida(
                        dto.IdPartida,
                        resultado.IdJugador))
                {
                    return BadRequest(new
                    {
                        Mensaje = "Uno de los jugadores no existe o no pertenece a esta partida."
                    });
                }

                // Si se envió el nombre, comprueba que coincida con el ID.
                if (!string.IsNullOrWhiteSpace(resultado.NombreJugador) &&
                    !string.Equals(
                        jugador.Nombre,
                        resultado.NombreJugador.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        Mensaje = "El nombre y el ID de uno de los jugadores no coinciden."
                    });
                }
            }

            // El ganador debe estar entre los tres jugadores y tener cero cartas.
            var ganador = dto.Resultados.FirstOrDefault(
                r => r.IdJugador == dto.IdJugadorGanador);

            if (ganador == null || ganador.CartasRestantes != 0)
            {
                return BadRequest(new
                {
                    Mensaje = "El ganador debe pertenecer a la partida y tener 0 cartas restantes."
                });
            }

            // Comprueba el nombre del ganador si fue enviado.
            if (!string.IsNullOrWhiteSpace(dto.NombreGanador))
            {
                var jugadorGanador =
                    _jugadorDAO.ObtenerPorId(dto.IdJugadorGanador);

                if (jugadorGanador == null ||
                    !string.Equals(
                        jugadorGanador.Nombre,
                        dto.NombreGanador.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        Mensaje = "El nombre del ganador no coincide con su ID."
                    });
                }
            }

            // La finalización y los resultados se guardan en una única transacción.
            bool finalizada = _historialDAO.FinalizarPartida(
                dto.IdPartida,
                dto.IdJugadorGanador,
                dto.Resultados.ToDictionary(
                    r => r.IdJugador,
                    r => r.CartasRestantes
                )
            );

            if (!finalizada)
            {
                return Conflict(new
                {
                    Mensaje = "La partida ya fue finalizada o no se pudo finalizar."
                });
            }

            return Ok(new
            {
                Mensaje = "Resultado final registrado correctamente."
            });
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