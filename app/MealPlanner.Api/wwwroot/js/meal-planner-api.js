const foodsUri = "/api/foods";
const plannerUri = "/api/planner/calculate";

let foods = [];

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
    foods = await response.json();

    displayFoods(foods);
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
    if (foods.length === 0) {
        await loadFoods();
    }

    const proteinFood = foods.find(x => x.category === "Protein");
    const carbFood = foods.find(x => x.category === "Carb");
    const fatFood = foods.find(x => x.category === "Fat");

    if (!proteinFood || !carbFood || !fatFood) {
        document.getElementById("planResult").innerHTML =
            "<p class='error'>Для розрахунку потрібен хоча б один продукт кожної категорії.</p>";
        return;
    }

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
        document.getElementById("planResult").innerHTML =
            "<p class='error'>Не вдалося розрахувати меню.</p>";
        return;
    }

    const result = await response.json();
    displayPlan(result);
}

function displayPlan(result) {
    const container = document.getElementById("planResult");

    let html = `
        <div class="summary">
            <b>Разом:</b>
            ${result.actualProtein} г білків,
            ${result.actualCarb} г вуглеводів,
            ${result.actualFat} г жирів,
            ${result.actualKcal} ккал
        </div>
    `;

    result.meals.forEach(meal => {
        html += `<h3>${meal.mealName}</h3>`;
        html += "<ul>";

        meal.items.forEach(item => {
            html += `
                <li>
                    ${item.foodName} — ${item.quantityValue} ${item.unitName}
                    (${item.role})
                </li>
            `;
        });

        html += "</ul>";
    });

    html += "<h3>Список покупок на 3 дні</h3><ul>";

    result.shoppingItems.forEach(item => {
        html += `<li>${item.foodName}: ${item.totalQuantityValue} ${item.unitName}</li>`;
    });

    html += "</ul>";

    container.innerHTML = html;
}

function clearOutput() {
    foods = [];
    document.getElementById("foodsBody").innerHTML = "";
    document.getElementById("counter").innerText = "";
    document.getElementById("planResult").innerHTML = "";
    document.getElementById("searchInput").value = "";
    document.getElementById("categoryInput").value = "";
}

function translateCategory(category) {
    if (category === "Protein") {
        return "Білковий";
    }

    if (category === "Carb") {
        return "Вуглеводний";
    }

    if (category === "Fat") {
        return "Жировий";
    }

    return category;
}

loadFoods();
