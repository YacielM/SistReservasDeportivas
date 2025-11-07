# 🏟️ Sistema de Reservas Deportivas

Proyecto académico desarrollado en **ASP.NET Core MVC + Web API + MySQL**, con autenticación mediante **Cookies (MVC)** y **JWT (API)**.  
Permite gestionar clientes, canchas, reservas y pagos, con validaciones de negocio, roles y documentación de API en Postman.

---

## 🚀 Tecnologías utilizadas
- **ASP.NET Core 8.0** (MVC + Web API)
- **Entity Framework Core** con **MySQL**
- **Autenticación**: Cookies (para MVC) + JWT (para API)
- **Bootstrap 5** para UI
- **Postman** para pruebas y documentación de API

---

## 📌 Funcionalidades principales
- Gestión de **Clientes** y **Canchas**
- Creación, edición y cancelación de **Reservas**
- Generación automática de **Pagos** al crear una reserva
- Validaciones de negocio:
  - Reserva mínima de 1 hora
  - Evitar solapamiento de reservas en la misma cancha
  - Cálculo automático del monto según precio/hora
- **Roles**: Administrador y Empleado
- **API REST** protegida con JWT
- **Búsqueda y paginación** en listados (ej: Reservas)

---

## ✅ Requerimientos mínimos y dónde están implementados

1. **Autenticación y Autorización**
   - ✔️ Implementado con **Cookies** para MVC (`Program.cs`)  
   - ✔️ Implementado con **JWT** para API (`Program.cs`, `AuthController`)  
   - ✔️ Políticas de roles definidas (`AdminOnly`, `EmpleadoOnly`)

2. **Gestión de entidades principales**
   - ✔️ CRUD de **Clientes** (`ClientesController`)  
   - ✔️ CRUD de **Canchas** (`CanchasController`)  
   - ✔️ CRUD de **Reservas** (`ReservasController`)  
   - ✔️ CRUD de **Pagos** (`PagosController`)

3. **Validaciones de negocio**
   - ✔️ Reserva mínima de 1 hora (`ReservasController.Create/Edit`)  
   - ✔️ Evitar solapamiento de reservas (`ReservasController.Create/Edit`)  

4. **Interfaz de usuario**
   - ✔️ Formularios con validaciones (`Views/Reservas/Create.cshtml`, etc.)  
   - ✔️ Bootstrap 5 aplicado en todas las vistas  
   - ✔️ Paginación en **Index de Reservas** (`ReservasController.Index`, `Views/Reservas/Index.cshtml`)

5. **API REST**
   - ✔️ Endpoints para Clientes, Canchas, Reservas y Pagos (`Controllers/Api/...`)  
   - ✔️ Protegidos con JWT (`[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`)  
   - ✔️ Endpoint de login que emite token (`AuthController.Login`)  
   - ✔️ Documentación en Postman (colección con ejemplos de uso)

6. **Persistencia**
   - ✔️ Base de datos MySQL (`appsettings.json`, `DataContext`)  
   - ✔️ Migraciones con EF Core

7. **Documentación**
   - ✔️ README con explicación del proyecto  
   - ✔️ Colección Postman con endpoints y ejemplos de uso

---

## 📖 Cómo probar el proyecto

1. Clonar el repositorio
   ```bash
   git clone https://github.com/usuario/sist-reservas-deportivas.git

   Configurar la base de datos en appsettings.json

    Configurar la base de datos en appsettings.json

    Ajustar la cadena de conexión a tu servidor MySQL.

    Ejecutar migraciones, eso crearía la estructura de tablas

        dotnet ef database update

    Levantar el proyecto

        dotnet run

Datos iniciales

    Si la base está vacía, al iniciar se crean automáticamente:

        Canchas de ejemplo

        Clientes de prueba

        Usuarios con roles:

            Administrador → admin@sist.com / admin123

            Empleado → empleado@sist.com / empleado123

    Si ya tenés datos cargados, el seed no los duplica.

Probar la API con Postman

    POST /api/auth/login → obtener token JWT

    Usar el token en los demás endpoints con el header:
        Authorization: Bearer {{token}}



