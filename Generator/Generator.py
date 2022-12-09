import random
import sys
import sympy as sympy

def shanks_tonally(n, p):

    n = n % p
    s = p - 1
    r = 0
    # получаем разложение p-1
    while s % 2 == 0:
        s //= 2
        r += 1
    # начальные значения: λ и ω
    l = pow(n, s, p)
    w = pow(n, (s + 1) // 2, p)
    # находим порядок λ
    mod = l
    m = 0
    while mod != 1:
        mod = mod * mod % p
        m += 1
    # находим квадратичный невычет
    z = 0
    i = 1
    while True:
        if legendre_symbol(i, p) == -1:
            z = i
            break
        i += 1
    # находим коэф-ты, на которые будем умножать
    yd_l = pow(pow(z, s, p), pow(2, r - m), p)
    yd_w = pow(pow(z, s, p), pow(2, r - m - 1), p)
    # находим корень
    while l != 1:
        l = l * yd_l % p
        w = w * yd_w % p
    return w


def complex_decomposition(D, p):

    assert D > 0, "Параметр D должен быть положительным"
    if legendre_symbol(-D, p) == -1:
        return None, None
    u = shanks_tonally(-D, p)
    i = 0
    u = [u]
    m = [p]
    while True:
        m.append((u[i] * u[i] + D) // m[i])
        u.append(min(u[i] % m[i + 1], (m[i + 1] - u[i]) % m[i + 1]))
        if m[i + 1] == 1:
            assert m[i] == u[i] * u[i] + D
            break
        i += 1
    a = [0] * (i + 1)
    a[i] = u[i]
    b = [0] * (i + 1)
    b[i] = 1
    while True:
        if i == 0:
            a = a[i]
            b = b[i]
            return a, b
        if (-u[i - 1]*a[i] + D*b[i]) % (a[i]*a[i] + D*b[i]*b[i]) == 0:
            a[i - 1] = (-u[i - 1]*a[i] + D*b[i]) // (a[i]*a[i] + D*b[i]*b[i])
        else:
            a[i - 1] = (u[i - 1]*a[i] + D*b[i]) // (a[i]*a[i] + D*b[i]*b[i])
        if (-a[i] - u[i - 1]*b[i]) % (a[i]*a[i] + D*b[i]*b[i]) == 0:
            b[i - 1] = (-a[i] - u[i - 1]*b[i]) // (a[i]*a[i] + D*b[i]*b[i])
        else:
            b[i - 1] = (-a[i] + u[i - 1] * b[i]) // (a[i] * a[i] + D * b[i] * b[i])
        i -= 1


def legendre_symbol(a, p):
    if a % p == 0:
        return 0
    if a == 1:
        return 1
    res = pow(a, (p - 1) // 2, p)
    if res != 1:
        res = -1
    return res


def extended_euclid(a, b):
    if b == 0:
        d = a
        x = 1
        y = 0
        return x, y, d

    x2 = 1
    x1 = 0
    y2 = 0
    y1 = 1
    while b > 0:
        q = a // b
        r = a - q * b
        x = x2 - q * x1
        y = y2 - q * y1
        a = b
        b = r
        x2 = x1
        x1 = x
        y2 = y1
        y1 = y
    d = a
    x = x2
    y = y2
    return x, y, d


def get_cubic_reciprocity(a, p):
    """ Вычисляет кубический вычет"""
    if (p % 3) == 2:
        return True
    if (p % 3) == 1:
        if (a == 0) or ((a ** int((p - 1) / 3)) % p == 1):
            return True
    return False


def get_prime(l):
    q = list(sympy.primerange(2 ** l, 2 ** (l + 1)))
    while True:
        qq = random.choice(q)
        if qq % 6 == 1:
            return qq


def isprime(n):
    return test_miller_rabin(n, 10)


def test_miller_rabin(n, K=10):
    def getST(t):
        s = 0
        while t % 2 == 0:
            t //= 2
            s += 1
        return s, t

    if n == 2 or n == 3:
        return True

    if n < 2 or n % 2 == 0:
        return False

    s, t = getST(n - 1)
    for k in range(K):
        a = random.randrange(2, n - 2)
        x = pow(a, t, n)
        if x == 1 or x == n - 1:
            continue
        for i in range(1, s):
            x = (x * x) % n
            if x == 1:
                return False
            if x == n - 1:
                break
        if x != n - 1:
            return False
    return True


def jacobi_symbol(m, n):
    return sympy.jacobi_symbol(m, n)


def is_int(n):
    return int(n) == float(n)


def calculate_nr(p, c, d):
    """ Проверить, что хотя бы для одного из чисел N=p+1+T, T=+-{c+3d, c-3d, 2c}
    выполняется одно из условий: N=r, N=2r, N=3r, N=6r, где r - простое число."""
    T = []
    for t in [c + 3 * d, c - 3 * d, 2 * c]:
        T.extend([t, -t])

    for t in T:
        N = p + 1 + t
        if isprime(N) and is_int(N):
            return N, N
        elif isprime(int(N / 2)) and is_int(N / 2):
            return N, N // 2
        elif isprime(int(N / 3)) and is_int(N / 3):
            return N, N // 3
        elif isprime(int(N / 6)) and is_int(N / 6):
            return N, N // 6
    return None, None


def verify_numbers(p, r, m):
    """ Проверить, что p!=r и p^i!=1 (mod r) для i=1..m"""
    if p == r:
        return False
    for i in range(1, m):
        if (p ** i) % r == 1:
            return False
    return True


def generate_start_point(p, n, r):
    """ Сгенерировать произвольную точку (x0, y0) такую, что x0!=0, y0!=0,
    вычислить коэффициент B <- y0^2-x0^3 (mod p). Проверить что выполняется
    одно из условий:
    B - квадратичный и кубический невычет для N = r,
    B - квадратичный и кубический вычет для N = 6r,
    B - квадратичный невычет и кубический вычет для N = 2r,
    B - квадратичный вычет и кубический невычет для N = 3r."""
    while True:
        x0 = random.randint(1, p)
        y0 = random.randint(1, p)
        b = ((y0 ** 2) - (x0 ** 3)) % p
        jac = jacobi_symbol(b, n)
        is_cubic = get_cubic_reciprocity(b, n)
        if n == r:
            if (jac == -1) and not is_cubic:
                return (x0, y0), b
        if n == 6 * r:
            if (jac == 1) and is_cubic:
                return (x0, y0), b
        if n == 2 * r:
            if (jac == -1) and is_cubic:
                return (x0, y0), b
        if n == 3 * r:
            if (jac == 1) and not is_cubic:
                return (x0, y0), b


def inverse(a, n):
    x, y, d = extended_euclid(a, n)
    if d == 1:
        x = (x % n + n) % n
        return x
    return 0


def equal_inv(p, q, mod):
    return p[0] == q[0] and p[1] == -q[1] % mod


def sum_points(P, Q, p):
    px, py = P
    qx, qy = Q
    if P == (-1, -1):
        return Q
    elif Q == (-1, -1):
        return P
    if equal_inv(P, Q, p):
        return (-1, -1)
    if P != Q:
        m = ((qy - py) % p) * inverse((qx - px) % p, p) % p
    else:
        m = (3 * px * px * inverse((2 * py) % p, p)) % p
    x3 = (m * m - px - qx) % p
    y3 = (-py + m * (px - x3)) % p
    return x3, y3


def mul_points(P, N, p):
    p_n = P
    p_q = (-1, -1)

    kbin = bin(N)[2:]
    m = len(kbin)
    for i in range(m):
        if kbin[m - i - 1] == '1':
            p_q = sum_points(p_q, p_n, p)
        p_n = sum_points(p_n, p_n, p)
    return p_q


def generate_points(p, N, r):
    while True:
        e, b = generate_start_point(p, N, r)
        print('Random point (x0, y0): (%s, %s)' % (e[0], e[1]))
        """ Шаг 6: Проверить, что N*(x0, y0) = P (Знаменатель углового 
        коэффициента в формуле сложения обращается в 0)"""
        m = mul_points(e, N, p)
        if m == (-1, -1):
            return e, b


def generator_elliptic_curve(l, m):
    while True:
        p = get_prime(l)
        print('p: %s' % p)
        c, d = complex_decomposition(3, p)
        assert c * c + 3 * d * d == p
        print('c: %s d: %s' % (c, d))
        if not c and not d:
            continue
        N, r = calculate_nr(p, c, d)
        print('N: %s r: %s' % (N, r))
        if not N or not r:
            continue
        if verify_numbers(p, r, m):
            break

    e, B = generate_points(p, N, r)
    print('Point x0, y0 = ', e)
    Q = mul_points(e, N // r, p)
    return p, B, Q, r


def verify_point_related_to_curve(point, B, p):
    if point == (-1, -1):
        return True
    curve = lambda x, y: (y**2) % p == (x**3 + B) % p
    assert curve(*point), "Точка {} не принадлежит кривой".format(point)


def get_l():
    try:
        l = int(input("Enter L: "))
        if l < 0:
            print("Error. Incorrect input data.")
            sys.exit()
        if l == 1:
            print("Value of l too long")
            sys.exit()
        if l == 2:
            print("error")
            sys.exit()
    except ValueError:
        print("Error: Integer positive number was epected")
        sys.exit()
    return l

f = open("l.txt", "r")
l = int(f.read())

m = 2
p, B, Q, r = generator_elliptic_curve(l, m)

print('Result: p: {}, B: {}, Q: {}, r: {}'.format(p, B, Q, r))

f = open("общие_параметры.txt", "w")
f.write('p = {}\nA = {}\nQ = {}\nr = {}'.format(p, B, Q, r))
f.close()


