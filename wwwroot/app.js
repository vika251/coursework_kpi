const API_BASE = '/api';

// Функція для форматування JSON-відповіді FluentValidation у читабельний рядок
function formatValidationErrors(errorJson) {
    let message = "Помилки валідації:\n";
    let hasErrors = false;
    
    // FluentValidation повертає словник {Поле: [Список помилок]}
    for (const field in errorJson) {
        if (Array.isArray(errorJson[field])) {
            errorJson[field].forEach(error => {
                message += `- ${error}\n`;
                hasErrors = true;
            });
        }
    }
    
    // Якщо вдалося знайти помилки, повертаємо відформатований текст. 
    return hasErrors ? message : JSON.stringify(errorJson, null, 2);
}

// --- НАВІГАЦІЯ ---
function showSection(sectionId) {
    document.querySelectorAll('main section').forEach(el => el.style.display = 'none');
    document.getElementById(`${sectionId}-section`).style.display = 'block';
    
    document.querySelectorAll('nav button').forEach(btn => btn.classList.remove('active'));
    const activeBtn = document.querySelector(`button[onclick="showSection('${sectionId}')"]`);
    if(activeBtn) activeBtn.classList.add('active');
    
    if (sectionId === 'pastries') loadPastries();
    if (sectionId === 'customers') loadCustomers();
    if (sectionId === 'orders') loadOrders();
}

// --- УНІВЕРСАЛЬНІ ФУНКЦІЇ ---

// 🔍 GET BY ID
async function findItemById(type) {
    let inputFieldId;

    // Визначаємо правильний ID поля вводу
    if (type === 'pastries') {
        inputFieldId = 'search-pastry-id';
    } else if (type === 'customers') {
        inputFieldId = 'search-customer-id';
    } else if (type === 'orders') {
        inputFieldId = 'search-order-id';
    } else {
        return; // Якщо тип невідомий
    }

    // Отримуємо значення, використовуючи коректний ID
    const id = document.getElementById(inputFieldId).value;

    if (!id) {
        alert("Введіть ID!");
        return;
    }

    try {
        // Виконуємо запит до API
        const res = await fetch(`${API_BASE}/${type}/${id}`);
        
        if (!res.ok) {
            // Перевіряємо статус 404 (Не знайдено)
            if (res.status === 404) {
                 alert(`Запис з ID ${id} не знайдено.`);
                 return;
            }
            throw new Error(`Помилка сервера: ${res.status}`);
        }
        
        const item = await res.json();
        
        // Відображаємо тільки один знайдений елемент
        const dataArray = [item];

        if (type === 'pastries') {
            renderPastries(dataArray);
        } else if (type === 'customers') {
            renderCustomers(dataArray);
        } else if (type === 'orders') {
            renderOrders(dataArray);
        }

    } catch (e) {
        console.error("Помилка пошуку:", e);
        alert("Помилка пошуку або з'єднання.");
    }
}

// ⚠️ DELETE ALL
async function deleteAll(type) {
    if (!confirm(`Ви точно хочете видалити ВСІ дані з категорії ${type}? Це незворотно!`)) return;

    const res = await fetch(`${API_BASE}/${type}`, { method: 'DELETE' });
    
    if (res.ok) {
        alert("Всі дані успішно видалено!");
        if (type === 'pastries') loadPastries();
        if (type === 'customers') loadCustomers();
        if (type === 'orders') loadOrders();
    } else {
        alert("Помилка видалення");
    }
}

// 🗑️ DELETE ONE
async function deleteItem(endpoint, id) {
    if (!confirm('Видалити цей запис?')) return;
    
    const res = await fetch(`${API_BASE}/${endpoint}/${id}`, { method: 'DELETE' });

    if (res.ok) {
        if (endpoint === 'pastries') loadPastries();
        if (endpoint === 'customers') loadCustomers();
        if (endpoint === 'orders') loadOrders();
    } else {
        const errorMsg = await res.text(); 
        alert("Помилка видалення: " + errorMsg);
    }
}

// --- ВИРОБИ (PASTRIES) ---

async function loadPastries() {
    const res = await fetch(`${API_BASE}/pastries`);
    const data = await res.json();
    renderPastries(data);
}

function renderPastries(data) {
    const tbody = document.querySelector('#pastries-table tbody');
    tbody.innerHTML = '';
    data.forEach(p => {
        tbody.innerHTML += `
            <tr>
                <td>${p.id}</td>
                <td>${p.name}</td>
                <td>${p.price} грн</td>
                <td>
                    <button class="edit-btn" onclick="editPastry(${p.id}, '${p.name}', ${p.price})">✏️</button>
                    <button class="delete-btn" onclick="deleteItem('pastries', ${p.id})">🗑️</button>
                </td>
            </tr>`;
    });
}

function editPastry(id, name, price) {
    document.getElementById('pastry-id').value = id;
    document.getElementById('pastry-name').value = name;
    document.getElementById('pastry-price').value = price;
}

function clearPastryForm() {
    document.getElementById('pastry-id').value = '';
    document.getElementById('pastry-name').value = '';
    document.getElementById('pastry-price').value = '';
}

async function savePastry() {
    const id = document.getElementById('pastry-id').value;
    const name = document.getElementById('pastry-name').value;
    const price = document.getElementById('pastry-price').value;

    const method = id ? 'PUT' : 'POST';
    const url = id ? `${API_BASE}/pastries/${id}` : `${API_BASE}/pastries`;
    const body = { name, price: parseFloat(price) };

    try {
        const res = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        // --- УСПІХ ---
        if (res.ok) {
            alert(id ? "Виріб оновлено!" : "Виріб створено!");
            clearPastryForm();
            loadPastries();
            return;
        }

        // --- БЛОК ОБРОБКИ ПОМИЛОК ---
        
        // 1. Отримуємо вміст відповіді як текст ОДИН РАЗ
        const errText = await res.text(); 
        let displayMessage;
        
        try {
            // 2. Спробуємо розпарсити текст як JSON (для FluentValidation)
            const errorJson = JSON.parse(errText);
            
            // 3. Якщо JSON успішний, форматуємо повідомлення
            displayMessage = formatValidationErrors(errorJson);
            
        } catch (e) {
            // 4. Якщо це не JSON (наприклад, проста помилка 409 Conflict або 404 Not Found)
            // Виводимо сирий текст або статус
            displayMessage = `Помилка ${res.status}: ${errText || res.statusText}`;
        }
        
        alert(displayMessage); 
        return; 

    } catch (e) {
        // Обробка мережевих помилок (якщо fetch не вдається підключитися)
        alert("Помилка з'єднання: " + e.message);
    }
}

// --- КЛІЄНТИ (CUSTOMERS) ---

async function loadCustomers() {
    const res = await fetch(`${API_BASE}/customers`);
    const data = await res.json();
    renderCustomers(data);
}

function renderCustomers(data) {
    const tbody = document.querySelector('#customers-table tbody');
    tbody.innerHTML = '';
    data.forEach(c => {
        tbody.innerHTML += `
            <tr>
                <td>${c.id}</td>
                <td>${c.name}</td>
                <td>${c.phone}</td>
                <td>
                    <button class="edit-btn" onclick="editCustomer(${c.id}, '${c.name}', '${c.phone}')">✏️</button>
                    <button class="delete-btn" onclick="deleteItem('customers', ${c.id})">🗑️</button>
                </td>
            </tr>`;
    });
}

async function saveCustomer() {
    const id = document.getElementById('customer-id').value;
    const name = document.getElementById('customer-name').value;
    const phone = document.getElementById('customer-phone').value;

    const method = id ? 'PUT' : 'POST';
    const url = id ? `${API_BASE}/customers/${id}` : `${API_BASE}/customers`;
    const body = { name, phone };

    try {
        const res = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        if (!res.ok) {
            const err = await res.text();
            
            // Тут також можна використати formatValidationErrors для красивого виведення:
            // try {
            //     const errorJson = JSON.parse(err);
            //     alert(formatValidationErrors(errorJson));
            // } catch (e) {
            //     alert("Помилка: " + err);
            // }
            alert("Помилка: " + err);
            return;
        }
        clearCustomerForm();
        loadCustomers();
    } catch (e) { alert("Помилка з'єднання"); }
}

function editCustomer(id, name, phone) {
    document.getElementById('customer-id').value = id;
    document.getElementById('customer-name').value = name;
    document.getElementById('customer-phone').value = phone;
}

function clearCustomerForm() {
    document.getElementById('customer-id').value = '';
    document.getElementById('customer-name').value = '';
    document.getElementById('customer-phone').value = '';
}

// --- ЗАМОВЛЕННЯ (ORDERS)  ---

async function loadOrders() {
    const res = await fetch(`${API_BASE}/orders`);
    const data = await res.json();
    renderOrders(data);
}

function renderOrders(data) {
    const tbody = document.querySelector('#orders-table tbody');
    tbody.innerHTML = '';
    data.forEach(o => {
        // Форматуємо дату
        const date = new Date(o.orderTime).toLocaleString();
        tbody.innerHTML += `
            <tr>
                <td>${o.id}</td>
                <td>${o.customerId}</td>
                <td><span class="status-badge">${o.status}</span></td>
                <td>${date}</td>
                <td>
                    <button class="edit-btn" onclick="editOrder(${o.id})">✏️</button>
                    <button class="delete-btn" onclick="deleteItem('orders', ${o.id})">🗑️</button>
                </td>
            </tr>`;
    });
}

// Функція для завантаження даних замовлення у форму для редагування
async function editOrder(id) {
    try {
        const res = await fetch(`${API_BASE}/orders/${id}`);
        if (!res.ok) return alert("Не вдалося завантажити замовлення");
        
        const order = await res.json();
        
        // Заповнюємо форму
        document.getElementById('order-form-title').innerText = `Редагування замовлення #${order.id}`;
        document.getElementById('order-id').value = order.id;
        document.getElementById('order-customer-id').value = order.customerId;
        document.getElementById('order-status').value = order.status;
        
        // Очищаємо і заповнюємо товари
        const container = document.getElementById('order-items-inputs');
        container.innerHTML = ''; 
        
        if (order.items && order.items.length > 0) {
            order.items.forEach(item => {
                addOrderItemRow(item.pastryId, item.quantity);
            });
        } else {
            addOrderItemRow();
        }
        
        // Прокручуємо до форми
        document.getElementById('orders-section').scrollIntoView({ behavior: 'smooth' });
        
    } catch (e) {
        console.error(e);
    }
}

function addOrderItemRow(pastryId = '', quantity = 1) {
    const container = document.getElementById('order-items-inputs');
    const div = document.createElement('div');
    div.className = 'order-item-row';
    div.innerHTML = `
        <input type="number" class="item-pastry-id" placeholder="ID Виробу" value="${pastryId}">
        <input type="number" class="item-quantity" placeholder="Кількість" value="${quantity}" min="1">
        <button onclick="removeRow(this)" class="remove-row-btn">❌</button>
    `;
    container.appendChild(div);
}

function removeRow(btn) {
    btn.parentElement.remove();
}

function clearOrderForm() {
    document.getElementById('order-form-title').innerText = '✨ Нове замовлення';
    document.getElementById('order-id').value = '';
    document.getElementById('order-customer-id').value = '';
    document.getElementById('order-status').value = 'Нове';
    document.getElementById('order-items-inputs').innerHTML = '';
    addOrderItemRow();
}

async function saveOrder() {
    const id = document.getElementById('order-id').value;
    const customerId = document.getElementById('order-customer-id').value;
    const status = document.getElementById('order-status').value;
    
    const items = [];
    document.querySelectorAll('.order-item-row').forEach(row => {
        const pId = row.querySelector('.item-pastry-id').value;
        const qty = row.querySelector('.item-quantity').value;
        if (pId && qty) {
            items.push({ pastryId: parseInt(pId), quantity: parseInt(qty) });
        }
    });

    if (!customerId || items.length === 0) {
        alert("Вкажіть ID клієнта та хоча б один товар!");
        return;
    }

    const body = {
        customerId: parseInt(customerId),
        status: status,
        items: items
    };

    const method = id ? 'PUT' : 'POST';
    const url = id ? `${API_BASE}/orders/${id}` : `${API_BASE}/orders`;

    const res = await fetch(url, {
        method: method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });

    if (res.ok) {
        alert(id ? "Замовлення оновлено!" : "Замовлення створено!");
        clearOrderForm();
        loadOrders();
    } else {
        const err = await res.text();
        alert("Помилка: " + err);
    }
}

// Завантаження при старті
loadPastries();