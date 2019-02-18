document.getElementById("start-search").addEventListener("click", (e) => {
    e.preventDefault();
    const query = document.getElementById("search-term").value;
    if (query.empty || query.length < 4) {
        alert("You must supply a search term of at least 4 characters.");
        return;
    }

    const categories = document.querySelectorAll("input[type='checkbox']");
    let queryString = "";
    queryString += "query=" + query;
    let categoryFound = false;

    categories.forEach((item) => {
        if (item.checked) {
            categoryFound = true;
            queryString += "&type=" + item.id;
        }
    });

    if (!categoryFound) {
        alert("You must select at least one category.");
        return;
    }

    window.location.href = "https://" + window.location.host + "/Home/Search/" + queryString;
});