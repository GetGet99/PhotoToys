import numbers
class StaticCounter:
    c = -1
    @staticmethod
    def getandnext():
        StaticCounter.c += 1
        return StaticCounter.c
    @staticmethod
    def reset():
        StaticCounter.c = -1
class MathActionRecorder:
    def __init__(self):
        self.history : list[tuple[int,str,object]] = []
    def __add__(self, o):
        self.history.append((StaticCounter.getandnext(), "add", o))
        return self
    def __radd__(self, o):
        self.history.append((StaticCounter.getandnext(),"radd", o))
        return self
    def __sub__(self, o):
        self.history.append((StaticCounter.getandnext(), "sub", o))
        return self
    def __rsub__(self, o):
        self.history.append((StaticCounter.getandnext(),"rsub", o))
        return self
    def __mul__(self, o):
        self.history.append((StaticCounter.getandnext(), "mul", o))
        return self
    def __rmul__(self, o):
        self.history.append((StaticCounter.getandnext(),"rmul", o))
        return self
    def __truediv__(self, o):
        self.history.append((StaticCounter.getandnext(), "truediv", o))
        return self
    def __rtruediv__(self, o):
        self.history.append((StaticCounter.getandnext(),"rtruediv", o))
        return self
    def __pow__(self, o):
        if not isinstance(type(o), numbers.Number):
            raise TypeError(None, "You can only power with number")
        self.history.append((StaticCounter.getandnext(),"pow", o))
        return self
    def showHistory(self):
        return "\n".join(f'{x[0]} {x[1]} {x[2]}' for x in self.history)
