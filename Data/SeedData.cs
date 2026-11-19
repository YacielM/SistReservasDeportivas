using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Models;

namespace SistReservasDeportivas.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            // Aplicar migraciones pendientes automáticamente
            context.Database.Migrate();

            // 1. Canchas
            if (!context.Canchas.Any())
            {
                context.Canchas.AddRange(
                    new Cancha { Nombre = "Cancha 1 - Sintético", Tipo = "Fútbol 5", PrecioHora = 12000, Estado = "Disponible", Foto = null },
                    new Cancha { Nombre = "Cancha 2 - Techada", Tipo = "Fútbol 5", PrecioHora = 15000, Estado = "Disponible", Foto = null },
                    new Cancha { Nombre = "Estadio Principal", Tipo = "Fútbol 7", PrecioHora = 22000, Estado = "Disponible", Foto = null },
                    new Cancha { Nombre = "Court Central", Tipo = "Tenis", PrecioHora = 8500, Estado = "Disponible", Foto = null },
                    new Cancha { Nombre = "Pádel Pro 1", Tipo = "Pádel", PrecioHora = 9000, Estado = "Disponible", Foto = null },
                    new Cancha { Nombre = "Pádel Pro 2", Tipo = "Pádel", PrecioHora = 9000, Estado = "Mantenimiento", Foto = null },
                    new Cancha { Nombre = "Polideportivo", Tipo = "Básquet", PrecioHora = 14000, Estado = "Disponible", Foto = null }
                );
                context.SaveChanges();
            }

            // 2. Clientes
            if (!context.Clientes.Any())
            {
                context.Clientes.AddRange(
                    new Cliente { Dni = "35441223", Nombre = "Juan", Apellido = "Pérez", FechaNacimiento = new DateTime(1990, 5, 10), Telefono = "2664112233", Email = "juan.perez@example.com" },
                    new Cliente { Dni = "28993441", Nombre = "María", Apellido = "Gómez", FechaNacimiento = new DateTime(1985, 3, 22), Telefono = "2664223344", Email = "maria.gomez@example.com" },
                    new Cliente { Dni = "41223554", Nombre = "Carlos", Apellido = "Ramos", FechaNacimiento = new DateTime(2000, 7, 15), Telefono = "2664334455", Email = "carlos.ramos@example.com" },
                    new Cliente { Dni = "37112998", Nombre = "Ana", Apellido = "Torres", FechaNacimiento = new DateTime(1993, 11, 4), Telefono = "2664445566", Email = "ana.torres@example.com" },
                    new Cliente { Dni = "32556778", Nombre = "Esteban", Apellido = "Quito", FechaNacimiento = new DateTime(1988, 9, 19), Telefono = "2664556677", Email = "esteban.quito@example.com" },
                    new Cliente { Dni = "39887112", Nombre = "Lucía", Apellido = "Fernández", FechaNacimiento = new DateTime(1997, 12, 1), Telefono = "2664667788", Email = "lucia.fernandez@example.com" }
                );
                context.SaveChanges();
            }

            // 3. Usuarios (Admin y Empleados)
            if (!context.Usuarios.Any())
            {
                var hasher = new PasswordHasher<Usuario>();

                var admin = new Usuario
                {
                    Nombre = "Administrador",
                    Apellido = "General",
                    Email = "admin@sist.com",
                    Rol = "Administrador"
                };
                admin.Clave = hasher.HashPassword(admin, "admin123");

                var empleado = new Usuario
                {
                    Nombre = "Martín",
                    Apellido = "Empleado",
                    Email = "empleado@sist.com",
                    Rol = "Empleado"
                };
                empleado.Clave = hasher.HashPassword(empleado, "empleado123");

                context.Usuarios.AddRange(admin, empleado);
                context.SaveChanges();
            }

            // 4. Reservas y Pagos Históricos / Activos / Cancelados
            if (!context.Reservas.Any())
            {
                var clientes = context.Clientes.ToList();
                var canchas = context.Canchas.ToList();

                var reservasSeed = new List<Reserva>();

                // Reserva 1: Finalizada y Pagada (Hace 3 días)
                var r1 = new Reserva
                {
                    IdCliente = clientes[0].IdCliente,
                    IdCancha = canchas[0].IdCancha,
                    Fecha = DateTime.Today.AddDays(-3),
                    HoraInicio = new TimeSpan(14, 0, 0),
                    HoraFin = new TimeSpan(15, 0, 0),
                    Monto = canchas[0].PrecioHora,
                    CreadoPor = "admin@sist.com",
                    Cancelada = false
                };
                context.Reservas.Add(r1);
                context.SaveChanges();

                context.Pagos.Add(new Pago
                {
                    IdReserva = r1.IdReserva,
                    Importe = r1.Monto,
                    Fecha = DateTime.Today.AddDays(-3),
                    MetodoPago = "Efectivo",
                    EstadoPago = "Pagado",
                    CreadoPor = "admin@sist.com"
                });

                // Reserva 2: Finalizada y Pagada (Ayer)
                var r2 = new Reserva
                {
                    IdCliente = clientes[1].IdCliente,
                    IdCancha = canchas[3].IdCancha,
                    Fecha = DateTime.Today.AddDays(-1),
                    HoraInicio = new TimeSpan(18, 0, 0),
                    HoraFin = new TimeSpan(19, 0, 0),
                    Monto = canchas[3].PrecioHora,
                    CreadoPor = "empleado@sist.com",
                    Cancelada = false
                };
                context.Reservas.Add(r2);
                context.SaveChanges();

                context.Pagos.Add(new Pago
                {
                    IdReserva = r2.IdReserva,
                    Importe = r2.Monto,
                    Fecha = DateTime.Today.AddDays(-1),
                    MetodoPago = "Transferencia",
                    EstadoPago = "Pagado",
                    CreadoPor = "empleado@sist.com"
                });

                // Reserva 3: Activa para Hoy (Pendiente de pago)
                var r3 = new Reserva
                {
                    IdCliente = clientes[2].IdCliente,
                    IdCancha = canchas[1].IdCancha,
                    Fecha = DateTime.Today,
                    HoraInicio = new TimeSpan(20, 0, 0),
                    HoraFin = new TimeSpan(21, 0, 0),
                    Monto = canchas[1].PrecioHora,
                    CreadoPor = "admin@sist.com",
                    Cancelada = false
                };
                context.Reservas.Add(r3);
                context.SaveChanges();

                context.Pagos.Add(new Pago
                {
                    IdReserva = r3.IdReserva,
                    Importe = r3.Monto,
                    Fecha = DateTime.Now,
                    MetodoPago = "Sin registrar",
                    EstadoPago = "Pendiente",
                    CreadoPor = "admin@sist.com"
                });

                // Reserva 4: Activa para Mañana (Pagada)
                var r4 = new Reserva
                {
                    IdCliente = clientes[3].IdCliente,
                    IdCancha = canchas[2].IdCancha,
                    Fecha = DateTime.Today.AddDays(1),
                    HoraInicio = new TimeSpan(19, 0, 0),
                    HoraFin = new TimeSpan(21, 0, 0), // 2 horas
                    Monto = canchas[2].PrecioHora * 2,
                    CreadoPor = "empleado@sist.com",
                    Cancelada = false
                };
                context.Reservas.Add(r4);
                context.SaveChanges();

                context.Pagos.Add(new Pago
                {
                    IdReserva = r4.IdReserva,
                    Importe = r4.Monto,
                    Fecha = DateTime.Now,
                    MetodoPago = "Tarjeta",
                    EstadoPago = "Pagado",
                    CreadoPor = "empleado@sist.com"
                });

                // Reserva 5: Cancelada (Soft Delete) con pago Anulado
                var r5 = new Reserva
                {
                    IdCliente = clientes[4].IdCliente,
                    IdCancha = canchas[4].IdCancha,
                    Fecha = DateTime.Today.AddDays(2),
                    HoraInicio = new TimeSpan(10, 0, 0),
                    HoraFin = new TimeSpan(11, 0, 0),
                    Monto = canchas[4].PrecioHora,
                    CreadoPor = "admin@sist.com",
                    Cancelada = true
                };
                context.Reservas.Add(r5);
                context.SaveChanges();

                context.Pagos.Add(new Pago
                {
                    IdReserva = r5.IdReserva,
                    Importe = r5.Monto,
                    Fecha = DateTime.Now,
                    MetodoPago = "Sin registrar",
                    EstadoPago = "Anulado",
                    CreadoPor = "admin@sist.com"
                });

                context.SaveChanges();
            }
        }
    }
}