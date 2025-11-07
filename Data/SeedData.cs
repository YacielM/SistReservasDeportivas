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

            // aplicar migraciones pendientes
            context.Database.Migrate();

            // Canchas
            if (!context.Canchas.Any())
            {
                context.Canchas.AddRange(
                    new Cancha { Nombre = "Cancha 1", Tipo = "Fútbol 5", PrecioHora = 5000, Estado = "Disponible" },
                    new Cancha { Nombre = "Cancha 2", Tipo = "Tenis", PrecioHora = 3000, Estado = "Disponible" },
                    new Cancha { Nombre = "Cancha 3", Tipo = "Pádel", PrecioHora = 4500, Estado = "Disponible" }
                );
                context.SaveChanges();
            }

            // Clientes
            if (!context.Clientes.Any())
            {
                context.Clientes.AddRange(
                    new Cliente { Dni = "12345678", Nombre = "Juan", Apellido = "Pérez", FechaNacimiento = new DateTime(1990, 5, 10), Telefono = "2664000001", Email = "juan@example.com" },
                    new Cliente { Dni = "87654321", Nombre = "María", Apellido = "Gómez", FechaNacimiento = new DateTime(1985, 3, 22), Telefono = "2664000002", Email = "maria@example.com" },
                    new Cliente { Dni = "11223344", Nombre = "Carlos", Apellido = "Ramos", FechaNacimiento = new DateTime(2000, 7, 15), Telefono = "2664000003", Email = "carlos@example.com" }
                );
                context.SaveChanges();
            }

            // Usuarios
            if (!context.Usuarios.Any())
            {
                var hasher = new PasswordHasher<Usuario>();

                var admin = new Usuario
                {
                    Nombre = "Admin",
                    Apellido = "Sistema",
                    Email = "admin@sist.com",
                    Rol = "Administrador"
                };
                admin.Clave = hasher.HashPassword(admin, "admin123");

                var empleado = new Usuario
                {
                    Nombre = "Empleado",
                    Apellido = "Sistema",
                    Email = "empleado@sist.com",
                    Rol = "Empleado"
                };
                empleado.Clave = hasher.HashPassword(empleado, "empleado123");

                context.Usuarios.AddRange(admin, empleado);
                context.SaveChanges();
            }

            // Reserva + Pago de ejemplo
            if (!context.Reservas.Any())
            {
                var cliente = context.Clientes.First();
                var cancha = context.Canchas.First();

                var reserva = new Reserva
                {
                    IdCliente = cliente.IdCliente,
                    IdCancha = cancha.IdCancha,
                    Fecha = DateTime.Today.AddDays(1),
                    HoraInicio = new TimeSpan(18, 0, 0),
                    HoraFin = new TimeSpan(19, 0, 0),
                    Monto = cancha.PrecioHora,
                    CreadoPor = "Sistema",
                    Cancelada = false
                };

                context.Reservas.Add(reserva);
                context.SaveChanges();

                var pago = new Pago
                {
                    IdReserva = reserva.IdReserva,
                    Importe = reserva.Monto,
                    Fecha = DateTime.Now,
                    CreadoPor = "Sistema"
                };

                context.Pagos.Add(pago);
                context.SaveChanges();
            }
        }
    }
}
