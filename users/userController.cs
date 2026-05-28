using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using fletesProyect.Patient;
using fletesProyect.Worker;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using project.Models;
using project.users.dto;
using project.users.Models;
using project.utils;
using project.utils.dto;
using project.utils.services;

namespace project.users
{
    [ApiController]
    [Route("user")]
    // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
    public class userController : controllerCommons<userEntity, userUpdateDto, userDto, userQueryDto, object, string>
    {
        //Esto nos va servir para crear nuevos usuarios 
        private readonly UserManager<userEntity> userManager;
        private readonly IConfiguration configuration;
        // Esto nos va a servir para el login
        private readonly SignInManager<userEntity> signManager;
        private readonly emailService emailService;
        private readonly IDataProtector dataProtector;
        protected override bool showDeleted { get; set; } = true;
        protected readonly userSvc userSvc;
        public userController(UserManager<userEntity> userManager, IConfiguration configuration, SignInManager<userEntity> signManager, emailService emailService, IDataProtectionProvider dataProvider, DBProyContext contex, IMapper mapper, userSvc userSvc) : base(contex, mapper)
        {
            this.userSvc = userSvc;
            this.userManager = userManager;
            this.configuration = configuration;
            this.signManager = signManager;
            this.emailService = emailService;
            this.dataProtector = dataProvider.CreateProtector(configuration["keyResetPasswordKey"]);

        }
        public override async Task<ActionResult> put(userUpdateDto user, [FromRoute] string id, [FromQuery] object queryCreation)
        {
            userEntity userDB = await userManager.FindByNameAsync(id);

            if (user.status)
            {
                userDB.deleteAt = null;
            }
            else if (userDB.deleteAt == null)
            {
                userDB.deleteAt = DateTime.Now.ToUniversalTime();
            }
            await userManager.UpdateAsync(userDB);


            if (userDB == null)
                return NotFound();
            IList<string> rolesCurrent = await userManager.GetRolesAsync(userDB);
            IList<string> rolesAdd = user.roles.Except(rolesCurrent).ToList();
            IList<string> rolesRemove = rolesCurrent.Except(user.roles).ToList();
            if (rolesRemove.Count != 0)
            {
                IdentityResult resultRemove = await userManager.RemoveFromRolesAsync(userDB, rolesRemove);
                if (!resultRemove.Succeeded)
                    return BadRequest(resultRemove.Errors);
            }

            if (rolesAdd.Count != 0)
            {
                IdentityResult resultAdd = await userManager.AddToRolesAsync(userDB, rolesAdd);
                if (!resultAdd.Succeeded)
                    return BadRequest(resultAdd.Errors);
            }

            return NoContent();
        }


        [HttpGet("{userName}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]

        public async Task<ActionResult<userDto>> getByUserName(string userName)
        {
            userEntity user = await userManager.FindByNameAsync(userName);
            if (user == null)
                return NotFound();
            IList<string> roles = await userManager.GetRolesAsync(user);
            userDto userDto = mapper.Map<userDto>(user);
            userDto.roles = roles.ToList();
            return userDto;
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
        public override async Task<ActionResult> delete(string id)
        {
            userEntity user = await userManager.FindByNameAsync(id);
            if (user == null)
                return NotFound();
            user.deleteAt = DateTime.Now.ToUniversalTime();
            await userManager.UpdateAsync(user);
            return NoContent();
        }


        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> register(clientCreationDto newCliente)
        {

            userCreationDto credentials = mapper.Map<userCreationDto>(newCliente);
            errorMessageDto error = await userSvc.register(credentials, new List<string> { "userNormal" });
            if (error != null)
                return BadRequest(error);
            userEntity newUser = await userManager.FindByEmailAsync(newCliente.email);
            Patient cliente = mapper.Map<Patient>(newCliente);
            cliente.userId = newUser.Id;
            context.Patients.Add(cliente);
            await context.SaveChangesAsync();
            return NoContent();
        }

        protected override async Task<IQueryable<userEntity>> modifyGet(IQueryable<userEntity> query, userQueryDto queryParam)
        {

            if (queryParam.roles != null)
            {
                List<string> userWithRoles = await context.UserRoles
                    .Where(userRole => queryParam.roles.Contains(userRole.RoleId))
                    .Select(userRole => userRole.UserId)
                    .ToListAsync();
                query = query.Where(userDB => userWithRoles.Contains(userDB.Id));
            }
            if (queryParam.isActive.HasValue)
            {
                if (queryParam.isActive.Value)
                    query = query.Where(userDB => userDB.deleteAt == null);
                else
                    query = query.Where(userDB => userDB.deleteAt != null);
            }
            return query;
        }

        [HttpPost("forgotPassword")]
        [AllowAnonymous]
        public async Task<ActionResult> forgotPassword([FromBody] emailDto email)
        {
            userEntity user = await userManager.FindByEmailAsync(email.email);

            if (user == null)
                NoContent();

            if (await userManager.IsEmailConfirmedAsync(user) == false)
                NoContent();

            if (user.deleteAt != null)
                NoContent();

            string token = await userManager.GeneratePasswordResetTokenAsync(user);
            string emailEncrypt = dataProtector.Protect(email.email);
            string tokenEncrypt = dataProtector.Protect(token);
            string encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(tokenEncrypt));

            emailService.SendEmail(new emailSendDto
            {
                email = email.email,
                subject = "Recuperar contraseña Aeropuerto",
                message = $"<h1>Recuperar contraseña</h1> <a href='{configuration["FrontUrl"]}/user/resetPassword/{emailEncrypt}/{encodedToken}'>Recuperar contraseña</a>"
            });
            return NoContent();
        }

        [HttpPost("resetPassword")]
        [AllowAnonymous]
        public async Task<ActionResult> resetPassword([FromBody] resetPasswordDto resetPassword)
        {
            string email = dataProtector.Unprotect(resetPassword.email);
            userEntity user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return BadRequest(new errorMessageDto("El enlace para restablecer la contraseña es inválido o ha expirado."));
            byte[] decodedToken = WebEncoders.Base64UrlDecode(resetPassword.token);
            string normalToken = Encoding.UTF8.GetString(decodedToken);
            string tokenDescrypted = dataProtector.Unprotect(normalToken);
            IdentityResult result = await userManager.ResetPasswordAsync(user, tokenDescrypted, resetPassword.password);
            if (result.Succeeded)
                return Ok();
            else
                return BadRequest(result.Errors);
        }


        [HttpGet("confirmEmail")]
        [AllowAnonymous]
        public async Task<ActionResult> confirmEmail([FromQuery] string email, [FromQuery] string token)
        {
            userEntity user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return BadRequest("Usuario no encontrado");
            byte[] decodedToken = WebEncoders.Base64UrlDecode(token);
            string normalToken = Encoding.UTF8.GetString(decodedToken);
            IdentityResult result = await userManager.ConfirmEmailAsync(user, normalToken);
            if (result.Succeeded)
                return Ok();
            else
                return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<authenticationDto>> login(credentialsDto credentials)
        {

            userEntity EMAIL = await userManager.FindByEmailAsync(credentials.email);
            if (EMAIL == null)
                return BadRequest(new errorMessageDto("Credenciales invalidas"));

            if (await userManager.IsEmailConfirmedAsync(EMAIL) == false)
                return BadRequest(new errorMessageDto("Credenciales invalidas"));

            if (EMAIL.deleteAt != null)
                return BadRequest(new errorMessageDto("Credenciales invalidas"));

            var result = await signManager.PasswordSignInAsync(EMAIL.UserName, credentials.password, false, false);
            if (result.Succeeded)
                return await createToken(credentials);
            else
                return BadRequest(new errorMessageDto("Credenciales invalidas"));
        }

        // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        // [HttpPost("renewToken")]
        // public async Task<ActionResult<authenticationDto>> renewToken()
        // {
        //     Claim email = HttpContext.User.Claims.Where(claim => claim.Type == "email").FirstOrDefault();
        //     credentialsDto credential = new credentialsDto
        //     {
        //         email = email.Value
        //     };
        //     return await createToken(credential);
        // }
        private async Task<authenticationDto> createToken(credentialsDto credentials)
        {
            userEntity user = await userManager.FindByEmailAsync(credentials.email);
            IList<Claim> claimUser = await userManager.GetClaimsAsync(user);
            IList<string> roles = await userManager.GetRolesAsync(user);
            foreach (string rol in roles)
            {
                claimUser.Add(new Claim(ClaimTypes.Role, rol));
            }
            claimUser.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claimUser.Add(new Claim(ClaimTypes.Name, user.UserName)); // Agrega el nombre de usuario como un claim
            Patient cliente = await context.Patients.Where(c => c.userId == user.Id && c.deleteAt == null).FirstOrDefaultAsync();
            if (cliente != null)
            {
                claimUser.Add(new Claim("patientId", cliente.Id.ToString()));
            }
            Worker driverThis = await context.Workers.Where(e => e.userId == user.Id && e.deleteAt == null).FirstOrDefaultAsync();
            if (driverThis != null)
            {
                claimUser.Add(new Claim("workerId", driverThis.Id.ToString()));
            }

            // Estos son los parametros que guardara el webToken
            List<Claim> claims = new List<Claim>(){
                new Claim("email", credentials.email),
            };
            claims.AddRange(claimUser);

            // Creamos nuestra sensual llave
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["keyJwt"]));
            SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            // Creamos la fecha de expiracion
            DateTime expiration = DateTime.UtcNow.AddDays(1);

            // Creamos nuestro token
            JwtSecurityToken securityToken = new JwtSecurityToken(issuer: null, audience: null, claims: claims, expires: expiration, signingCredentials: creds);
            return new authenticationDto()
            {
                token = new JwtSecurityTokenHandler().WriteToken(securityToken),
                expiration = expiration
            };
        }
    }
}