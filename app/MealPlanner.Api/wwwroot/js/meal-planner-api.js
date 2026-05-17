const foodsUri = "/api/Foods";
const plannerUri = "/api/Planner/calculate";

let displayedFoods = [];
let allFoods = [];

async function loadFoods() {
    const search = document.getElementById("searchInput").value.trim();
    const category = document.getElementById("categoryInput").value;

    const params = new URLSearchParams();

    if (search.length > 0) {
        params.append("search", search);
    }

    if (category.length > 0) {
        params.append("category", category);
    }

    const url = params.toString().length > 0
        ? `${foodsUri}?${params.toString()}`
        : foodsUri;

    const response = await fetch(url);

    if (!response.ok) {
        showPlanError("Не вдалося завантажити продукти.");
        return;
    }

    displayedFoods = await response.json();
    displayFoods(displayedFoods);
}

async function loadAllFoods() {
    const response = await fetch(foodsUri);

    if (!response.ok) {
        showPlanError("Не вдалося завантажити повний список продуктів.");
        return [];
    }

    allFoods = await response.json();
    return allFoods;
}

function displayFoods(data) {
    const body = document.getElementById("foodsBody");
    const counter = document.getElementById("counter");

    body.innerHTML = "";
    counter.innerText = `Знайдено продуктів: ${data.length}`;

    data.forEach(food => {
        const row = body.insertRow();

        row.insertCell(0).innerText = food.name;
        row.insertCell(1).innerText = translateCategory(food.category);
        row.insertCell(2).innerText = food.proteinPer100;
        row.insertCell(3).innerText = food.carbsPer100;
        row.insertCell(4).innerText = food.fatPer100;
        row.insertCell(5).innerText = food.kcalPer100;
    });
}

async function calculateDemoPlan() {
    const foodsForCalculation = await loadAllFoods();

    const proteinFood = foodsForCalculation.find(x => isCategory(x.category, "Protein"));
    const carbFood = foodsForCalculation.find(x => isCategory(x.category, "Carb"));
    const fatFood = foodsForCalculation.find(x => isCategory(x.category, "Fat"));

    if (!proteinFood || !carbFood || !fatFood) {
        document.getElementById("selectedFoodsInfo").innerHTML = "";
        showPlanError("Для розрахунку потрібен хоча б один продукт кожної категорії: білковий, вуглеводний і жировий.");
        return;
    }

    document.getElementById("selectedFoodsInfo").innerHTML = `
        <div class="summary">
            <b>Для розрахунку обрано:</b><br>
            Білковий продукт: ${proteinFood.name}<br>
            Вуглеводний продукт: ${carbFood.name}<br>
            Жировий продукт: ${fatFood.name}
        </div>
    `;

    const request = {
        userId: proteinFood.userId,
        days: 3,
        mealsPerDay: Number(document.getElementById("mealsPerDay").value),
        proteinTarget: Number(document.getElementById("proteinTarget").value),
        carbTarget: Number(document.getElementById("carbTarget").value),
        fatTarget: Number(document.getElementById("fatTarget").value),
        proteinFoodIds: [proteinFood.id],
        carbFoodIds: [carbFood.id],
        fatFoodIds: [fatFood.id]
    };

    const response = await fetch(plannerUri, {
        method: "POST",
        headers: {
            "Accept": "application/json",
            "Content-Type": "application/json"
        },
        body: JSON.stringify(request)
    });

    if (!response.ok) {
        const errorText = await response.text();
        showPlanError(`Не вдалося розрахувати меню. ${errorText}`);
        return;
    }

    const result = await response.json();
    displayPlan(result);
}

function displayPlan(result) {
    const container = document.getElementById("planResult");

    let html = `
        <div class="summary">
            <b>Разом за 1 день:</b><br>
            Білки: ${result.actualProtein} г<br>
            Вуглеводи: ${result.actualCarb} г<br>
            Жири: ${result.actualFat} г<br>
            Калорійність: ${result.actualKcal} ккал
        </div>
    `;

    result.meals.forEach(meal => {
        html += `<h3>${meal.mealName}</h3>`;
        html += `
            <table>
                <thead>
                    <tr>
                        <th>Продукт</th>
                        <th>Роль</th>
                        <th>Кількість</th>
                        <th>Білки</th>
                        <th>Вуглеводи</th>
                        <th>Жири</th>
                        <th>Ккал</th>
                    </tr>
                </thead>
                <tbody>
        `;

        meal.items.forEach(item => {
            html += `
                <tr>
                    <td>${item.foodName}</td>
                    <td>${item.role}</td>
                    <td>${item.quantityValue} ${item.unitName}</td>
                    <td>${item.protein}</td>
                    <td>${item.carb}</td>
                    <td>${item.fat}</td>
                    <td>${item.kcal}</td>
                </tr>
            `;
        });

        html += `
                </tbody>
            </table>
        `;
    });

    html += "<h3>Список покупок на 3 дні</h3>";
    html += `
        <table>
            <thead>
                <tr>
                    <th>Продукт</th>
                    <th>Кількість</th>
                </tr>
            </thead>
            <tbody>
    `;

    result.shoppingItems.forEach(item => {
        html += `
            <tr>
                <td>${item.foodName}</td>
                <td>${item.totalQuantityValue} ${item.unitName}</td>
            </tr>
        `;
    });

    html += `
            </tbody>
        </table>
    `;

    container.innerHTML = html;
}

function clearOutput() {
    displayedFoods = [];
    allFoods = [];
    document.getElementById("foodsBody").innerHTML = "";
    document.getElementById("counter").innerText = "";
    document.getElementById("selectedFoodsInfo").innerHTML = "";
    document.getElementById("planResult").innerHTML = "";
    document.getElementById("searchInput").value = "";
    document.getElementById("categoryInput").value = "";
}

function showPlanError(message) {
    document.getElementById("planResult").innerHTML = `<p class="error">${message}</p>`;
}

function isCategory(value, categoryName) {
    if (categoryName === "Protein") {
        return value === "Protein" || value === 1;
    }

    if (categoryName === "Carb") {
        return value === "Carb" || value === 2;
    }

    if (categoryName === "Fat") {
        return value === "Fat" || value === 3;
    }

    return false;
}

function translateCategory(category) {
    if (category === "Protein" || category === 1) {
        return "Білковий";
    }

    if (category === "Carb" || category === 2) {
        return "Вуглеводний";
    }

    if (category === "Fat" || category === 3) {
        return "Жировий";
    }

    return category;
}

loadFoods();
