using BookonnectAPI.Models;

namespace BookonnectAPI.Lib;

public interface IMailLibrary
{
    public Task SendMail(Email email);
}

