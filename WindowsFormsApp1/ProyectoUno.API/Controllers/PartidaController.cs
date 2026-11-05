using System;
using System.Collections.Generic;
using System.Linq;
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
            if (dto.NombresJugadores == null ||
                dto.NombresJugadores.Count != 3)
            {
                return BadRequest(new
                {
                    Mensaje = "La partida debe tener exactamente 3 jugadores."
                });
            }

            List<string> nombres = dto.NombresJugadores.Select(n => n?.Trim()).ToList();

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

            string[] accionesPermitidas =
            {
                "Salto", "Reversa", "+2", "+4"
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

            string[] coloresPermitidos =
            {
                "Rojo", "Azul", "Amarillo", "Verde"
            };

            if (!coloresPermitidos.Contains(dto.NuevoColor))
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
                dto.NuevoColor
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
            if (dto.Resultados == null || dto.Resultados.Count != 3)
            {
                return BadRequest(new
                {
                    Mensaje = "Se deben enviar los resultados de los 3 jugadores."
                });
            }

            if (dto.Resultados.Any(r =>
                string.IsNullOrWhiteSpace(r.NombreJugador)))
            {
                return BadRequest(new
                {
                    Mensaje = "Todos los resultados deben tener un nombre de jugador."
                });
            }

            var nombres = dto.Resultados
                .Select(r => r.NombreJugador.Trim())
                .ToList();

            if (nombres.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
            {
                return BadRequest(new
                {
                    Mensaje = "No se permiten jugadores repetidos en los resultados."
                });
            }

            var ganador = dto.Resultados.FirstOrDefault(r =>
                string.Equals(
                    r.NombreJugador.Trim(),
                    dto.NombreGanador.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            );

            if (ganador == null || ganador.CartasRestantes != 0)
            {
                return BadRequest(new
                {
                    Mensaje = "El ganador debe estar en los resultados y tener 0 cartas restantes."
                });
            }

            foreach (var resultado in dto.Resultados)
            {
                if (_jugadorDAO.ObtenerPorNombre(resultado.NombreJugador.Trim()) == null)
                {
                    return BadRequest(new
                    {
                        Mensaje = "Uno de los jugadores indicados no existe."
                    });
                }
            }

            foreach (var resultado in dto.Resultados)
            {
                _historialDAO.GuardarResultado(
                    dto.IdPartida,
                    resultado.NombreJugador.Trim(),
                    resultado.CartasRestantes
                );
            }

            _historialDAO.GuardarGanador(
                dto.IdPartida,
                dto.NombreGanador.Trim()
            );

            _historialDAO.RegistrarGanadasPerdidas(
                dto.IdPartida,
                dto.NombreGanador.Trim()
            );

            return Ok(new
            {
                Mensaje = "Resultado final registrado correctamente."
            });
        }
        private bool JugadorValido(int idJugador, string nombreJugador)
        {
            if (idJugador <= 0 || string.IsNullOrWhiteSpace(nombreJugador))
                return false;

            var jugador = _jugadorDAO.ObtenerPorId(idJugador);

            return jugador != null &&
                   string.Equals(
                       jugador.Nombre,
                       nombreJugador.Trim(),
                       StringComparison.OrdinalIgnoreCase
                   );
        }
    }
}