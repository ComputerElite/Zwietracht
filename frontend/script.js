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

function SafeFormat(text) {
    return text.replace(/</g, "&lt;").replace(/>/g, "&gt;")
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