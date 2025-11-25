namespace ConfectioneryApi.Services
{
    // Цей клас допомагає сервісу повернути і дані, і статус успіху/помилки
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public T? Data { get; set; }

        // Успішний результат
        public static ServiceResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
        
        // Результат з помилкою
        public static ServiceResult<T> Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
    }
}