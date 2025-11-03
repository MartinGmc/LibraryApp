window.loginUser = async function(loginData) {
    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            credentials: 'include', // Important: include cookies
            body: loginData
        });

        if (response.ok) {
            return 'success';
        } else {
            const errorData = await response.json();
            return errorData.message || 'Invalid login credentials. Please try again.';
        }
    } catch (error) {
        return 'An error occurred: ' + error.message;
    }
};

window.logoutUser = async function() {
    try {
        const response = await fetch('/api/auth/logout', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            credentials: 'include', // Important: include cookies
        });

        if (response.ok) {
            return 'success';
        } else {
            const errorData = await response.json();
            return errorData.message || 'Logout failed. Please try again.';
        }
    } catch (error) {
        return 'An error occurred: ' + error.message;
    }
};

