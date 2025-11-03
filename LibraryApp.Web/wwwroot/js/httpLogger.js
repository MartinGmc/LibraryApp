// HTTP Request Logger for Browser Console
// Intercept fetch API calls made by Blazor's HttpClient to log them to console

(function() {
    const originalFetch = window.fetch;
    
    window.fetch = async function(url, options = {}) {
        const method = options.method || 'GET';
        const startTime = Date.now();
        const timestamp = new Date().toLocaleTimeString();
        
        // Extract request body if present
        let requestBody = null;
        if (options.body) {
            if (typeof options.body === 'string') {
                requestBody = options.body;
            } else if (options.body instanceof FormData) {
                requestBody = '[FormData]';
            } else if (options.body instanceof URLSearchParams) {
                requestBody = options.body.toString();
            } else if (options.body instanceof ReadableStream) {
                requestBody = '[Stream]';
            }
        }
        
        // Log request start
        console.log(`%c[HTTP Request] ${method} ${url} @ ${timestamp}`, 'color: blue; font-weight: bold;');
        if (requestBody) {
            try {
                const parsed = JSON.parse(requestBody);
                console.log('%cRequest:', 'color: blue;', parsed);
            } catch {
                console.log('%cRequest:', 'color: blue;', requestBody);
            }
        }
        
        try {
            // Make the actual request
            const response = await originalFetch.apply(this, arguments);
            const duration = Date.now() - startTime;
            
            // Clone the response to read the body
            const clonedResponse = response.clone();
            const statusCode = response.status;
            
            // Determine log style based on status code
            const style = statusCode >= 400 ? 'color: red; font-weight: bold;' : 
                          statusCode >= 300 ? 'color: orange;' : 
                          'color: green;';
            
            // Log response
            console.group(`%c[HTTP ${statusCode}] ${method} ${url}`, style);
            console.log(`%cTimestamp: ${timestamp} | Duration: ${duration}ms`, 'color: gray; font-size: 0.9em;');
            
            // Try to read response body (only for text/json responses)
            const contentType = response.headers.get('content-type') || '';
            if (contentType.includes('application/json') || contentType.includes('text/')) {
                try {
                    const responseBody = await clonedResponse.text();
                    if (responseBody) {
                        try {
                            const parsed = JSON.parse(responseBody);
                            console.log('%cResponse Body:', 'color: green; font-weight: bold;', parsed);
                        } catch {
                            console.log('%cResponse Body:', 'color: green; font-weight: bold;', responseBody.substring(0, 200));
                        }
                    }
                } catch {
                    // Couldn't read response body
                }
            }
            
            console.groupEnd();
            
            return response;
        } catch (error) {
            const duration = Date.now() - startTime;
            console.error(`%c[HTTP Error] ${method} ${url} after ${duration}ms`, 'color: red; font-weight: bold;', error);
            throw error;
        }
    };
    
    console.log('HTTP Request Logger initialized - intercepting fetch API calls');
    
    // Intercept WebSocket connections (used by SignalR/Blazor Server)
    const originalWebSocket = window.WebSocket;
    
    window.WebSocket = function(url, protocols) {
        const timestamp = new Date().toLocaleTimeString();
        const urlString = typeof url === 'string' ? url : String(url);
        const isBlazorConnection = urlString.includes('/_blazor');
        
        // Log connection attempt
        if (isBlazorConnection) {
            console.log(`%c[WebSocket] Blazor connecting to ${urlString} @ ${timestamp}`, 'color: purple; font-weight: bold;');
        } else {
            console.log(`%c[WebSocket] Connecting to ${urlString} @ ${timestamp}`, 'color: purple; font-weight: bold;');
        }
        
        // Create the WebSocket instance
        const ws = protocols ? new originalWebSocket(url, protocols) : new originalWebSocket(url);
        
        // For Blazor connections, use passive logging (only event listeners, no method wrapping)
        // Wrapping send() breaks SignalR, so we avoid it for Blazor
        if (isBlazorConnection) {
            // Log when connection opens
            ws.addEventListener('open', function(event) {
                const openTime = new Date().toLocaleTimeString();
                console.log(`%c[WebSocket] Blazor connected @ ${openTime}`, 'color: green; font-weight: bold;');
            });
            
            // Log messages received over WebSocket (read-only, safe)
            /*ws.addEventListener('message', async function(event) {
                const receiveTime = new Date().toLocaleTimeString();
                console.group(`%c[WebSocket] Blazor message received @ ${receiveTime}`, 'color: green; font-weight: bold;');
                try {
                    const data = event.data;
                    
                    if (typeof data === 'string') {
                        // Truncate long messages to avoid console spam
                        const displayData = data.length > 500 ? data.substring(0, 500) + '...' : data;
                        try {
                            const parsed = JSON.parse(data);
                            console.log('%cData (JSON):', 'color: green; font-weight: bold;', parsed);
                        } catch {
                            console.log('%cData (Text):', 'color: green; font-weight: bold;', displayData);
                        }
                    } else {
                        // Handle binary data (Blazor Server uses MessagePack binary protocol)
                        let arrayBuffer;
                        
                        if (data instanceof ArrayBuffer) {
                            arrayBuffer = data;
                        } else if (data instanceof Blob) {
                            // Convert Blob to ArrayBuffer asynchronously
                            arrayBuffer = await data.arrayBuffer();
                        } else if (data instanceof Uint8Array) {
                            arrayBuffer = data.buffer;
                        } else {
                            // Try to access directly
                            arrayBuffer = data;
                        }
                        
                        const byteLength = arrayBuffer.byteLength || 0;
                        console.log(`%cBinary data length: ${byteLength} bytes`, 'color: gray;');
                        
                        // Convert to Uint8Array for inspection
                        const bytes = new Uint8Array(arrayBuffer);
                        
                        // Try to decode as UTF-8 string first (some parts might be text)
                        try {
                            const decoder = new TextDecoder('utf-8', { fatal: false });
                            const textAttempt = decoder.decode(bytes);
                            // Check if it contains readable text
                            const readableChars = textAttempt.split('').filter(c => /[\x20-\x7E]/.test(c)).length;
                            const readabilityRatio = readableChars / textAttempt.length;
                            
                            if (readabilityRatio > 0.3) { // If more than 30% readable characters
                                const textPreview = textAttempt.length > 500 ? textAttempt.substring(0, 500) + '...' : textAttempt;
                                console.log(`%cDecoded as UTF-8 text:`, 'color: yellow; font-weight: bold;', textPreview);
                                
                                // Try JSON parse
                                try {
                                    const jsonAttempt = JSON.parse(textAttempt);
                                    console.log('%cParsed as JSON:', 'color: green; font-weight: bold;', jsonAttempt);
                                } catch {
                                    // Not JSON, that's fine
                                }
                            }
                        } catch (e) {
                            // UTF-8 decode failed, continue with binary display
                        }
                        
                        // Show first bytes in hex (useful for debugging MessagePack)
                        const previewLength = Math.min(100, bytes.length);
                        const hexPreview = Array.from(bytes.slice(0, previewLength))
                            .map(b => b.toString(16).padStart(2, '0').toUpperCase())
                            .join(' ');
                        console.log(`%cHex (first ${previewLength} bytes):`, 'color: cyan; font-weight: bold;', hexPreview + (bytes.length > previewLength ? '...' : ''));
                        
                        // Show as base64 for easier inspection
                        if (bytes.length > 0) {
                            const base64Length = Math.min(200, bytes.length);
                            let base64 = '';
                            try {
                                // Convert bytes to base64
                                const chunk = bytes.slice(0, base64Length);
                                base64 = btoa(String.fromCharCode.apply(null, chunk));
                                if (bytes.length > base64Length) {
                                    base64 += '...';
                                }
                                console.log(`%cBase64 (first ${base64Length} bytes):`, 'color: cyan;', base64);
                            } catch (e) {
                                // Base64 conversion failed
                            }
                        }
                        
                        // Show full byte array if small enough
                        if (bytes.length <= 50) {
                            console.log('%cFull bytes array:', 'color: cyan;', Array.from(bytes));
                        }
                    }
                } catch (e) {
                    console.error('%cError processing message:', 'color: red;', e);
                }
                console.groupEnd();
            });*/
            
            // Log connection errors
            ws.addEventListener('error', function(event) {
                const errorTime = new Date().toLocaleTimeString();
                console.error(`%c[WebSocket] Blazor error @ ${errorTime}`, 'color: red; font-weight: bold;', event);
            });
            
            // Log when connection closes
            ws.addEventListener('close', function(event) {
                const closeTime = new Date().toLocaleTimeString();
                console.log(`%c[WebSocket] Blazor closed @ ${closeTime} (code: ${event.code}, reason: ${event.reason || ''})`, 'color: orange; font-weight: bold;');
            });
            
            // NOTE: We intentionally do NOT wrap ws.send() for Blazor connections
            // as it breaks SignalR's internal state management
        } else {
            // For non-Blazor connections, full logging including send interception
            // Log when connection opens
            ws.addEventListener('open', function(event) {
                const openTime = new Date().toLocaleTimeString();
                console.log(`%c[WebSocket] Connected @ ${openTime}`, 'color: green; font-weight: bold;');
            });
            
            // Log messages sent over WebSocket
            const originalSend = ws.send.bind(ws);
            ws.send = function(data) {
                const sendTime = new Date().toLocaleTimeString();
                console.group(`%c[WebSocket] Outgoing message @ ${sendTime}`, 'color: blue; font-weight: bold;');
                try {
                    if (typeof data === 'string') {
                        try {
                            const parsed = JSON.parse(data);
                            console.log('%cData:', 'color: blue;', parsed);
                        } catch {
                            console.log('%cData:', 'color: blue;', data);
                        }
                    } else {
                        console.log('%cData:', 'color: blue;', `[Binary data, length: ${data.byteLength}]`);
                    }
                } catch (e) {
                    console.log('%cData:', 'color: blue;', '[Unable to log]');
                }
                console.groupEnd();
                return originalSend(data);
            };
            
            // Log messages received over WebSocket
            ws.addEventListener('message', function(event) {
                const receiveTime = new Date().toLocaleTimeString();
                console.group(`%c[WebSocket] Incoming message @ ${receiveTime}`, 'color: green; font-weight: bold;');
                try {
                    const data = event.data;
                    if (typeof data === 'string') {
                        try {
                            const parsed = JSON.parse(data);
                            console.log('%cData:', 'color: green;', parsed);
                        } catch {
                            console.log('%cData:', 'color: green;', data);
                        }
                    } else {
                        console.log('%cData:', 'color: green;', `[Binary data, length: ${data.byteLength}]`);
                    }
                } catch (e) {
                    console.log('%cData:', 'color: green;', '[Unable to log]');
                }
                console.groupEnd();
            });
            
            // Log connection errors
            ws.addEventListener('error', function(event) {
                const errorTime = new Date().toLocaleTimeString();
                console.error(`%c[WebSocket] Error @ ${errorTime}`, 'color: red; font-weight: bold;', event);
            });
            
            // Log when connection closes
            ws.addEventListener('close', function(event) {
                const closeTime = new Date().toLocaleTimeString();
                console.log(`%c[WebSocket] Closed @ ${closeTime} (code: ${event.code}, reason: ${event.reason})`, 'color: orange; font-weight: bold;');
            });
        }
        
        return ws;
    };
    
    // Preserve WebSocket prototype and static properties
    window.WebSocket.prototype = originalWebSocket.prototype;
    Object.setPrototypeOf(window.WebSocket, originalWebSocket);
    Object.setPrototypeOf(window.WebSocket.prototype, originalWebSocket.prototype);
    
    console.log('WebSocket logger initialized - intercepting WebSocket connections');
})();
