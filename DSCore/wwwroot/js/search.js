document.getElementById("search-click").addEventListener("click", (e) => {
    if (document.getElementById("search-box").value.length > 3)
        window.location.href = 'https://' + window.location.host + '/Home/Search/' + document.getElementById("search-box").value;
    else
        alert("Search term must be more than three characters.")
})