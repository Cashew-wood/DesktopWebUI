const native = {};
window.native = native;
(async() => {
    function strToObjStruct(str, obj) {
        let root = native;
        let keys = str.split('.');
        for (let i = 0; i < keys.length; i++) {
            let key = keys[i];
            if (i == keys.length - 1 && !root[key]) root[key] = obj;
            else if (!root[key]) root[key] = {};
            root = root[key];
        }
        return keys[keys.length - 1];
    }
    let sessionId = 0;
    let queue = {};
    let prefix = '__'
    let ojectDescribesKey = prefix + 'oject_describes';
    let injection = ['window', 'window.parent', 'device'];

    native[ojectDescribesKey] = {};
    chrome.webview.addEventListener("message", e => {
        if (e.data.Type == 0) {
            if (e.data.Data) {
                for (let info of e.data.Data) {
                    let obj = { Field: info.Field, Type: info.Type };
                    native[ojectDescribesKey][e.data.ObjectName][prefix + info.Name] = obj;
                    if (queue[e.data.Id])
                        queue[e.data.Id].target[prefix + info.Name] = obj;
                    Object.defineProperty(native[ojectDescribesKey][e.data.ObjectName], prefix + info.Name, {
                        enumerable: false,
                        writable: true
                    })
                }
            }
            queue[e.data.Id] && queue[e.data.Id].callback(e.data.Data == null);
        } else if (e.data.Type == 1) {
            queue[e.data.Id] && queue[e.data.Id](e.data.Data);
        } else if (e.data.Type == 2) {
            queue[e.data.Id].method.call(queue[e.data.Id].that, e.data.Data);
        }
        delete queue[e.data.Id];
        for (let key in queue) {
            if (key < e.data.Id / 2 - 10) {
                delete queue[key];
            }
        }
    });
    async function register(key) {
        let target = {};
        native[ojectDescribesKey][key] = target;
        if (await new Promise((resolve, reject) => {
                queue[sessionId] = {
                    callback: resolve,
                    target
                };
                chrome.webview.postMessage({
                    Type: 0,
                    Id: sessionId++,
                    Name: key,
                    Args: []
                })
            })) return;
        strToObjStruct(key, new Proxy(target, {
            get: (target, property, receiver) => {
                if (target[prefix + property] == undefined) {
                    return target[property];
                }
                let type = 0;
                if (target[prefix + property].Type) {
                    type = target[prefix + property].Type * 10000;
                }
                if (target[prefix + property].Field) {
                    return new Promise((resolve, reject) => {
                        queue[sessionId] = resolve;
                        chrome.webview.postMessage({
                            Type: type + 1,
                            Id: sessionId++,
                            Name: key,
                            IsField: true,
                            Member: property,
                            Args: []
                        });
                    });
                } else {
                    return function() {
                        let funParams = arguments;
                        let that = this;
                        return new Promise((resolve, reject) => {
                            queue[sessionId] = resolve;
                            let args = [];
                            let funCount = 0;
                            for (let val of funParams) {
                                if (typeof(val) == 'function') {
                                    let key = sessionId + ((funCount + 1) * 0.01);
                                    args.push(['__function__', key]);
                                    queue[key] = { that, method: val };
                                }
                                args.push(val);
                            }
                            chrome.webview.postMessage({
                                Type: type + 1,
                                Id: sessionId++,
                                Name: key,
                                IsField: false,
                                Member: property,
                                Args: args
                            });
                        })
                    }
                }

            },
            set: (target, property, value) => {
                if (target[prefix + property] != undefined) {
                    let type = 0;
                    if (target[prefix + property].Type) {
                        type = target[prefix + property].Type * 10000;
                    }
                    chrome.webview.postMessage({
                        Type: type + 1,
                        Id: sessionId++,
                        Name: key,
                        IsField: true,
                        Member: property,
                        Args: [value]
                    });
                } else {
                    if (target[property] == undefined && !native[ojectDescribesKey][key + '.' + property]) { //hide not mian property：subwindows.window.*,parent.*
                        chrome.webview.postMessage({
                            Type: 3,
                            Name: key,
                            IsField: true,
                            Member: property,
                            Args: [typeof(value)]
                        });
                    }
                    target[property] = value;

                }
            }
        }));
        return target;
    }
    async function init() {
        for (let item of injection) {
            await register(item);
        }
        let mouseLeftDown = false;
        document.addEventListener('mousedown', (e) => {
            e.button == 0 & (mouseLeftDown = Date.now())
        })
        document.addEventListener('mousemove', (e) => {
            mouseLeftDown && (Date.now() - mouseLeftDown) > 50 && native.window.dragWindow(e.pageX, e.pageY)

        })
        document.addEventListener('mouseup', (e) => {
            e.button == 0 && (mouseLeftDown = 0);
        })
    }
    await init();
    native.window.createWindow = async(name, url, isHtml) => {
        if (isHtml == undefined && !url) {
            url = name;
            name = null;
            isHtml = null;
        } else if (isHtml == undefined) {
            isHtml = url;
            url = name;
            name = null
        }
        let key = await native.window._createWindow(name, url, isHtml);
        let window = strToObjStruct(key, await register(key));
        native.window.subwindows[window].parent = native.window;
        return native.window.subwindows[window];
    }
    native.window.data = new Proxy({}, {
        get: (target, property) => {
            return native.window.getData(property);
        },
        set: (target, property, value) => {
            native.window.setData(property, value);
        }
    })
    if (native.window.parent) {
        native.window.parent.data = new Proxy({}, {
            get: function(target, property) {
                return native.window.parent.getData(property);
            },
            set: function(target, property, value) {
                native.window.parent.setData(property, value);
            }
        })
    }
    window.dispatchEvent(new Event("native"));
    setTimeout(() => {
        chrome.webview.postMessage({ Type: -2 })
    }, 100);
})()