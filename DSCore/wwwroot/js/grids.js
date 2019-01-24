const columnSort = (array, parameter, desc) => {
    return array.sort((a, b) => {
        if (parameter == "price") {
            a.price = parseFloat(a.price)
            b.price = parseFloat(b.price)
        }

        if (!desc) {
            return a[parameter] > b[parameter] ? 1 : -1
            return 0
        }
        else {
            return a[parameter] < b[parameter] ? 1 : -1
            return 0
        }
    })
}

//app
const app = () => {
    const state = getState()

    const buttons = document.querySelectorAll(".btn-sort")

    let sortedState = []
    for (let each of buttons) {
        let parameter = each.classList.contains("name") ? "name" :
            each.classList.contains("price") ? "price" :
                each.classList.contains("volume") ? "volume" : undefined

        each.addEventListener('click', () => {
            sortedState = columnSort(
                state,
                parameter,
                each.classList.contains("sort-down") ? true : false
            )

            render(sortedState)
        })
    }
}

const getState = () => {
    const rowElements = document.getElementById("tbl").querySelector('tbody').querySelectorAll('tr')
    const rows = Array.from(rowElements)

    const data = rows.map((el) => {
        const cells = el.querySelectorAll("td")

        return {
            name: cells[0].textContent,
            price: cells[1].textContent,
            volume: cells[2].textContent
        }
    })

    return data
}

const render = (state) => {
    const tbody = document.getElementById("tbl").querySelector('tbody')

    while (tbody.firstChild) {
        tbody.removeChild(tbody.firstChild)
    }

    for (let each of state) {
        createTableRow(each)
    }

    function createTableRow(p) {
        let tr = document.createElement('tr')
        tbody.appendChild(tr)

        let name = document.createElement('td')
        tr.appendChild(name)
        name.textContent = p.name

        let price = document.createElement('td')
        tr.appendChild(price)
        price.textContent = p.price

        let volume = document.createElement('td')
        tr.appendChild(region)
        volume.textContent = p.volume
        volume.classList.add('volume')
        volume.classList.add(p.volume.toLowerCase())
    }
}

app()