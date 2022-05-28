function TextBoxError(id, text) {
    ChangeTextBoxProperty(id, "var(--red)", text)
}

function TextBoxText(id, text) {
    ChangeTextBoxProperty(id, "var(--highlightedColor)", text)
}

function TextBoxGood(id, text) {
    ChangeTextBoxProperty(id, "var(--textColor)", text)
}

function HideTextBox(id) {
    document.getElementById(id).style.visibility = "hidden"
}

function ChangeTextBoxProperty(id, color, innerHtml) {
    var text = document.getElementById(id)
    text.style.visibility = "visible"
    text.style.border = color + " 1px solid"
    text.innerHTML = innerHtml
}

function GetTimeString(d) {
    var date = new Date(d)
    if(date.toLocaleDateString() == new Date(Date.now()).toLocaleDateString()) {
        return date.toLocaleTimeString()
    }
    return date.toLocaleString()
}

function SafeFormat(text) {
    return Format(text.replace(/</g, "&lt;").replace(/>/g, "&gt;"))
}

function Format(res) {
    var links = []
    const websiteRegex = /https?\:\/\/([^ ()\n\t<>\\]+(\/)?)/g
    while ((match = websiteRegex.exec(res)) !== null) {
        var replacement = `<a href="${match[0]}" target="_blank">${match[0]}</a>`
        links.push({
            absolute: replacement,
            relative: match[0],
            start: match.index,
            end: websiteRegex.lastIndex
        })
    }
    var length = 0
    links.forEach(link => {
        res = res.substring(0, link.start + length) + res.substring(link.end + length, res.length)
        res = InsertString(link.absolute, link.start + length, res)
        length += link.absolute.length - link.relative.length
    })
    res = res.replace(/\\n/g, "<br>")
    res = res.replace(/\t/g, "&emsp;&emsp;&emsp;&emsp;")
    return res
}

function InsertString(toInsert, position, text) {
    return [text.slice(0, position), toInsert, text.slice(position)].join('')
}

function tfetch(url, method = "GET", body = "") {
    return ifetch(url, false, method, body)
}

function jfetch(url, method = "GET", body = "") {
    return ifetch(url, true, method, body)
}

function ifetch(url, asjson = true, method = "GET", body = "") {
    return new Promise((resolve, reject) => {
        if(method == "GET" || method == "HEAD") {
            fetch(url, {
                method: method,
                headers: {
                    "token": localStorage.token
                }
            }).then(res => {
                res.text().then(res => {
                    if(asjson) {
                        try {
                            resolve(JSON.parse(res))
                        } catch(e) {
                            reject(e)
                        }
                    } else {
                        resolve(res)
                    }
                })
            })
        } else {
            fetch(url, {
                method: method,
                body: body,
                headers: {
                    "token": localStorage.token
                }
            }).then(res => {
                res.text().then(res => {
                    if(asjson) {
                        try {
                            resolve(JSON.parse(res))
                        } catch(e) {
                            reject(e)
                        }
                    } else {
                        resolve(res)
                    }
                })
            })
        }
    })
}