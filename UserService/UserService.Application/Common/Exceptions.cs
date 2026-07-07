namespace UserService.Application.Common
{
    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException(string email)
            : base($"User with email '{email}' already exists.") { }
    }

    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string identifier)
            : base($"User '{identifier}' was not found.") { }
    }

    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException()
            : base("Invalid email or password.") { }
    }
}
