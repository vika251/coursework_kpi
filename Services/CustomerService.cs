using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Models;
using ConfectioneryApi.Repositories;
using ConfectioneryApi.Validators;
using FluentValidation;

namespace ConfectioneryApi.Services
{
    // Реалізація сервісу клієнтів.
    public class CustomerService : ICustomerService
    {
        private readonly IRepository<Customer> _repository;
        private readonly DeleteCustomerValidator _deleteValidator;

        public CustomerService(IRepository<Customer> repository, DeleteCustomerValidator deleteValidator)
        {
            _repository = repository;
            _deleteValidator = deleteValidator;
        }

        public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync()
        {
            var customers = await _repository.GetAllAsync();
            // Мапінг сутностей в DTO
            return customers.Select(c => new CustomerDto { Id = c.Id, Name = c.Name, Phone = c.Phone });
        }

        public async Task<ServiceResult<CustomerDto>> GetCustomerByIdAsync(int id)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer == null) return ServiceResult<CustomerDto>.Failure("Клієнта не знайдено");
            
            return ServiceResult<CustomerDto>.Success(new CustomerDto { Id = customer.Id, Name = customer.Name, Phone = customer.Phone });
        }

        public async Task<ServiceResult<CustomerDto>> CreateCustomerAsync(CreateCustomerDto createDto)
        {
            var newCustomer = new Customer { Name = createDto.Name, Phone = createDto.Phone };
            
            await _repository.AddAsync(newCustomer);
            await _repository.SaveChangesAsync();

            return ServiceResult<CustomerDto>.Success(new CustomerDto { Id = newCustomer.Id, Name = newCustomer.Name, Phone = newCustomer.Phone });
        }

        public async Task<ServiceResult<bool>> UpdateCustomerAsync(int id, UpdateCustomerDto updateDto)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer == null) return ServiceResult<bool>.Failure("Клієнта не знайдено");

            customer.Name = updateDto.Name;
            customer.Phone = updateDto.Phone;
            
            _repository.Update(customer);
            await _repository.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> DeleteCustomerAsync(int id)
        {
            // Валідація перед видаленням (чи є активні замовлення?)
            var validationResult = await _deleteValidator.ValidateAsync(id);
            if (!validationResult.IsValid) 
            {
                return ServiceResult<bool>.Failure(validationResult.Errors.First().ErrorMessage);
            }

            var customer = await _repository.GetByIdAsync(id);
            if (customer == null) return ServiceResult<bool>.Failure("Клієнта не знайдено");

            _repository.Delete(customer);
            await _repository.SaveChangesAsync();
            
            return ServiceResult<bool>.Success(true);
        }

        public async Task DeleteAllCustomersAsync()
        {
            await _repository.DeleteAllAsync();
        }
    }
}