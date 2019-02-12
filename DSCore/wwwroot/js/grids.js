const getCellValue = (tr, idx) =>
{
    let text = tr.children[idx].innerText || tr.children[idx].textContent; // Get the cell value
    if (text.match(/d/g)) // If the value contains a number
        text = text.replace(/D/g, ''); // Remove all none numbers
    return text; // Return the string that can now be sorted without issue
}

const comparer = (idx, asc) => (a, b) => ((v1, v2) =>
    v1 !== "" && v2 !== "" && !isNaN(v1) && !isNaN(v2) ? v1 - v2 : v1.toString().localeCompare(v2)
)(getCellValue(asc ? a : b, idx), getCellValue(asc ? b : a, idx));

const tables = document.getElementsByClassName("sort-table"); // Get all tables
for (var i = 0; i < tables.length; i++) // Loop over the tables
{
    tables[i].querySelectorAll("td").forEach(td => // Loop over all the values in the header
        td.addEventListener("click", ((link) => { // Add a click event to each of them
            const table = td.closest("table"); // Get the table this element is part of
            const tbody = table.querySelector('tbody'); // Get the body of that table
            Array.from(tbody.querySelectorAll('tr')) // Loop over all the rows
                .sort(comparer(Array.from(td.parentNode.children).indexOf(td), this.asc = !this.asc)) // Sort them when the header is clicked
                .forEach(tr => tbody.appendChild(tr)); // Replace

            link.preventDefault(); // Prevent anchor behaviour
        }))
    )
}